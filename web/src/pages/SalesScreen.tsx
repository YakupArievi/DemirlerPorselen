import { useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { api, apiErrorMessage } from '../api/client';
import type { ResolvedBarcode, Sale, SaleStatus, UnitType, Variant, Warehouse } from '../api/types';
import { tl, dateStr } from '../lib/format';
import { inputCls, btnPrimary, btnGhost } from '../components/Modal';
import { VariantPicker } from '../components/VariantPicker';
import { RowActions } from '../components/RowActions';
import { SaleReceiptModal } from '../components/SaleReceipt';

interface SaleListItem {
  id: string; saleNumber: number; customerName: string; saleDate: string;
  status: SaleStatus; grandTotal: number; paidAmount: number;
}
import { resolveBarcodeLocal, enqueue } from '../offline/sync';
import { useOffline } from '../offline/store';

interface CartItem {
  key: string; variantId: string; name: string; unitType: UnitType;
  quantity: number; unitPrice: number; lineDiscount: number;
}

export function SalesScreen() {
  const qc = useQueryClient();
  // Lookup: id+ad (Depocu dahil herkes erişebilir; parasal bilgi yok)
  const customers = useQuery({ queryKey: ['customers-lookup'], queryFn: async () => (await api.get<{ id: string; name: string }[]>('/customers/lookup')).data });
  const warehouses = useQuery({ queryKey: ['warehouses'], queryFn: async () => (await api.get<Warehouse[]>('/warehouses')).data });
  const recentSales = useQuery({ queryKey: ['recent-sales'], queryFn: async () => (await api.get<{ items: SaleListItem[] }>('/sales?pageSize=10')).data.items });

  // Yanlış girilen satış: silinmez, İPTAL edilir (stok + cari geri alınır). Doğrusu yeniden girilir.
  const cancelSale = useMutation({
    mutationFn: async (sid: string) => api.post(`/sales/${sid}/cancel`),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['recent-sales'] }); setMsg({ kind: 'ok', text: 'Satış iptal edildi (stok ve cari geri alındı).' }); },
    onError: (e) => setMsg({ kind: 'err', text: apiErrorMessage(e) }),
  });

  // Düzenle: satış doğrudan değiştirilemez (storno). Mevcut satışı iptal edip kalemlerini
  // sepete yükleriz; kullanıcı düzeltip yeniden tamamlar.
  const editSale = async (s: SaleListItem) => {
    if (!confirm(`#${s.saleNumber} düzenlenecek: mevcut satış iptal edilip kalemleri sepete yüklenir. Devam?`)) return;
    setMsg(null);
    try {
      const full = (await api.get<Sale>(`/sales/${s.id}`)).data;
      await api.post(`/sales/${s.id}/cancel`);
      qc.invalidateQueries({ queryKey: ['recent-sales'] });
      setCustomerId(full.customerId);
      setWarehouseId(full.warehouseId);
      setCart(full.lines.map((l) => ({
        key: crypto.randomUUID(), variantId: l.variantId,
        name: `${l.productName} (${l.unitType})`,
        unitType: l.unitType, quantity: l.quantity, unitPrice: l.unitPrice, lineDiscount: l.lineDiscount,
      })));
      setMsg({ kind: 'ok', text: `#${s.saleNumber} iptal edildi ve sepete yüklendi. Düzeltip "Satışı Tamamla"ya basın.` });
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (e) { setMsg({ kind: 'err', text: apiErrorMessage(e) }); }
  };

  const [customerId, setCustomerId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [barcode, setBarcode] = useState('');
  const [cart, setCart] = useState<CartItem[]>([]);
  const [docDiscount, setDocDiscount] = useState(0);
  const [payType, setPayType] = useState<'' | 'Nakit' | 'Kart' | 'Cek'>('');
  const [payAmount, setPayAmount] = useState(0);
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);
  const [receiptSaleId, setReceiptSaleId] = useState<string | null>(null);
  const barcodeRef = useRef<HTMLInputElement>(null);
  const refreshPending = useOffline((s) => s.refreshPending);

  useEffect(() => {
    if (warehouses.data && !warehouseId && warehouses.data.length > 0) {
      const def = warehouses.data.find((w) => w.isDefault) ?? warehouses.data[0];
      setWarehouseId(def.id);
    }
  }, [warehouses.data, warehouseId]);

  const addBarcode = async (code: string) => {
    if (!code.trim()) return;
    try {
      let r: ResolvedBarcode | null = null;
      if (navigator.onLine) {
        try {
          r = (await api.get<ResolvedBarcode>(`/variants/resolve?barcode=${encodeURIComponent(code.trim())}`)).data;
        } catch (err) {
          if (axios.isAxiosError(err) && err.response) throw err; // 404 vb. gerçek hata
          r = await resolveBarcodeLocal(code); // ağ hatası -> yerel
        }
      } else {
        r = await resolveBarcodeLocal(code);
      }
      if (!r) { setMsg({ kind: 'err', text: 'Barkod bulunamadı (çevrimdışı önbellekte de yok).' }); return; }
      setCart((c) => {
        const existing = c.find((i) => i.variantId === r.variantId && i.unitType === r.unitType);
        if (existing) return c.map((i) => i === existing ? { ...i, quantity: i.quantity + 1 } : i);
        const unitPrice = r.unitType === 'Koli' ? r.salePrice * r.adetEquivalent : r.salePrice;
        return [...c, {
          key: crypto.randomUUID(), variantId: r.variantId,
          name: `${r.productName} (${r.unitType})`,
          unitType: r.unitType, quantity: 1, unitPrice, lineDiscount: 0,
        }];
      });
      setBarcode('');
      barcodeRef.current?.focus();
    } catch (e) {
      setMsg({ kind: 'err', text: apiErrorMessage(e) });
    }
  };

  // Listeden ürün seç -> sepete ekle (adet bazlı, fiyat = satış fiyatı). Aynı ürün tekrar seçilirse adet artar.
  const addVariant = (v: Variant, unitType: UnitType = 'Adet') => {
    setCart((c) => {
      const existing = c.find((i) => i.variantId === v.id && i.unitType === unitType);
      if (existing) return c.map((i) => i === existing ? { ...i, quantity: i.quantity + 1 } : i);
      const unitPrice = unitType === 'Koli' ? v.salePrice * v.koliIciAdet : v.salePrice;
      return [...c, {
        key: crypto.randomUUID(), variantId: v.id,
        name: `${v.productName} (${unitType})`,
        unitType, quantity: 1, unitPrice, lineDiscount: 0,
      }];
    });
  };

  const update = (key: string, patch: Partial<CartItem>) =>
    setCart((c) => c.map((i) => i.key === key ? { ...i, ...patch } : i));
  const remove = (key: string) => setCart((c) => c.filter((i) => i.key !== key));

  const subTotal = cart.reduce((s, i) => s + i.unitPrice * i.quantity, 0);
  const lineDiscTotal = cart.reduce((s, i) => s + i.lineDiscount, 0);
  const grandTotal = subTotal - lineDiscTotal - docDiscount;

  const submit = async () => {
    setMsg(null);
    if (!customerId) return setMsg({ kind: 'err', text: 'Müşteri seçin.' });
    if (!warehouseId) return setMsg({ kind: 'err', text: 'Depo seçin.' });
    if (cart.length === 0) return setMsg({ kind: 'err', text: 'Sepet boş.' });
    setBusy(true);
    const opId = crypto.randomUUID();
    const body = {
      customerId, warehouseId, idempotencyKey: opId,
      documentDiscount: docDiscount,
      initialPayment: payType && payAmount > 0 ? { type: payType, amount: payAmount } : null,
      lines: cart.map((i) => ({ variantId: i.variantId, unitType: i.unitType, quantity: i.quantity, unitPrice: i.unitPrice, lineDiscount: i.lineDiscount })),
    };
    const queueIt = async () => {
      await enqueue('sale', '/sales', body, opId);
      await refreshPending();
      setMsg({ kind: 'ok', text: 'Çevrimdışı: satış kuyruğa alındı, bağlantı gelince gönderilecek.' });
      setCart([]); setDocDiscount(0); setPayType(''); setPayAmount(0);
    };
    try {
      if (!navigator.onLine) { await queueIt(); return; }
      const res = await api.post<Sale>('/sales', body);
      setMsg({ kind: 'ok', text: `Satış #${res.data.saleNumber} kaydedildi (${tl(res.data.grandTotal)}).` });
      setCart([]); setDocDiscount(0); setPayType(''); setPayAmount(0);
      qc.invalidateQueries({ queryKey: ['recent-sales'] });
    } catch (e) {
      // Ağ hatası -> kuyruğa al; iş kuralı hatası -> göster
      if (axios.isAxiosError(e) && !e.response) { await queueIt(); }
      else setMsg({ kind: 'err', text: apiErrorMessage(e) });
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-slate-800">Satış</h1>
      {msg && <div className={`rounded p-2 text-sm ${msg.kind === 'ok' ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'}`}>{msg.text}</div>}

      <div className="grid gap-4 lg:grid-cols-2">
        <label className="block">
          <span className="mb-1 block text-sm text-slate-600">Müşteri</span>
          <select className={inputCls} value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
            <option value="">Müşteri seçin...</option>
            {customers.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>
        <label className="block">
          <span className="mb-1 block text-sm text-slate-600">Depo</span>
          <select className={inputCls} value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
            <option value="">Depo seçin...</option>
            {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </label>
      </div>

      <div className="rounded-lg bg-white p-4 shadow">
        <span className="mb-1 block text-sm font-medium text-slate-700">Ürün Ekle</span>
        <VariantPicker onPick={(v) => addVariant(v, 'Adet')} />
        <div className="mt-2 flex items-center gap-2">
          <span className="text-xs text-slate-400">veya barkod:</span>
          <input ref={barcodeRef} className={inputCls + ' max-w-xs'} placeholder="Barkod okut + Enter" value={barcode}
            onChange={(e) => setBarcode(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') addBarcode(barcode); }} />
        </div>
      </div>

      <div className="overflow-hidden rounded-lg bg-white shadow">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-slate-500">
            <tr><th className="p-2">Ürün</th><th className="p-2">Birim</th><th className="p-2 w-24">Adet/Koli</th><th className="p-2 w-28">B.Fiyat</th><th className="p-2 w-28">İskonto</th><th className="p-2 text-right">Tutar</th><th></th></tr>
          </thead>
          <tbody>
            {cart.map((i) => (
              <tr key={i.key} className="border-t">
                <td className="p-2">{i.name}</td>
                <td className="p-2">{i.unitType}</td>
                <td className="p-2"><input type="number" className={inputCls} value={i.quantity === 0 ? '' : i.quantity} min={1} onChange={(e) => update(i.key, { quantity: e.target.value === '' ? 0 : +e.target.value })} /></td>
                <td className="p-2"><input type="number" className={inputCls} value={i.unitPrice === 0 ? '' : i.unitPrice} onChange={(e) => update(i.key, { unitPrice: e.target.value === '' ? 0 : +e.target.value })} /></td>
                <td className="p-2"><input type="number" className={inputCls} value={i.lineDiscount === 0 ? '' : i.lineDiscount} onChange={(e) => update(i.key, { lineDiscount: e.target.value === '' ? 0 : +e.target.value })} /></td>
                <td className="p-2 text-right">{tl(i.unitPrice * i.quantity - i.lineDiscount)}</td>
                <td className="p-2 text-right"><button className="text-red-500" onClick={() => remove(i.key)}>✕</button></td>
              </tr>
            ))}
            {cart.length === 0 && <tr><td colSpan={7} className="p-6 text-center text-slate-400">Sepet boş — barkod okutun</td></tr>}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-4 lg:flex-row lg:justify-end">
        <div className="rounded-lg bg-white p-4 shadow lg:w-96">
          <div className="flex justify-between py-1 text-sm"><span>Ara Toplam</span><span>{tl(subTotal)}</span></div>
          <div className="flex justify-between py-1 text-sm"><span>Satır İskontoları</span><span>-{tl(lineDiscTotal)}</span></div>
          <div className="flex items-center justify-between py-1 text-sm">
            <span>Fiş İskontosu</span>
            <input type="number" className={inputCls + ' w-28 text-right'} value={docDiscount === 0 ? '' : docDiscount} onChange={(e) => setDocDiscount(e.target.value === '' ? 0 : +e.target.value)} />
          </div>
          <div className="mt-2 flex justify-between border-t pt-2 text-lg font-bold"><span>Genel Toplam</span><span>{tl(grandTotal)}</span></div>

          <div className="mt-3 flex gap-2">
            <select className={inputCls} value={payType} onChange={(e) => setPayType(e.target.value as typeof payType)}>
              <option value="">Peşin ödeme yok</option>
              <option value="Nakit">Nakit</option><option value="Kart">Kart</option><option value="Cek">Çek</option>
            </select>
            <input type="number" className={inputCls + ' w-32'} placeholder="Tutar" value={payAmount === 0 ? '' : payAmount} onChange={(e) => setPayAmount(e.target.value === '' ? 0 : +e.target.value)} disabled={!payType} />
          </div>

          <div className="mt-3 flex gap-2">
            <button className={btnGhost + ' flex-1'} onClick={() => { setCart([]); setDocDiscount(0); }}>Temizle</button>
            <button className={btnPrimary + ' flex-1'} disabled={busy} onClick={submit}>Satışı Tamamla</button>
          </div>
        </div>
      </div>

      <div className="overflow-hidden rounded-lg bg-white shadow">
        <div className="border-b px-4 py-3 text-sm font-medium text-slate-700">Son Satışlar</div>
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-slate-500">
            <tr><th className="p-2">Fiş</th><th className="p-2">Müşteri</th><th className="p-2">Tarih</th><th className="p-2 text-right">Tutar</th><th className="p-2">Durum</th><th></th></tr>
          </thead>
          <tbody>
            {recentSales.data?.map((s) => (
              <tr key={s.id} className="border-t">
                <td className="p-2">#{s.saleNumber}</td>
                <td className="p-2">{s.customerName}</td>
                <td className="p-2">{dateStr(s.saleDate)}</td>
                <td className="p-2 text-right">{tl(s.grandTotal)}</td>
                <td className="p-2">{s.status === 'Cancelled' ? <span className="text-red-500">İptal</span> : <span className="text-emerald-600">Aktif</span>}</td>
                <td className="p-2 text-right">
                  <RowActions actions={[
                    { label: 'Fiş / Yazdır', onClick: () => setReceiptSaleId(s.id) },
                    ...(s.status === 'Active' ? [
                      { label: 'Düzenle', onClick: () => editSale(s) },
                      { label: 'İptal et', danger: true, onClick: () => { if (confirm(`#${s.saleNumber} satışı iptal edilsin mi? Stok ve cari geri alınır.`)) cancelSale.mutate(s.id); } },
                    ] : []),
                  ]} />
                </td>
              </tr>
            ))}
            {recentSales.data?.length === 0 && <tr><td colSpan={6} className="p-6 text-center text-slate-400">Henüz satış yok</td></tr>}
          </tbody>
        </table>
      </div>

      {receiptSaleId && <SaleReceiptModal saleId={receiptSaleId} onClose={() => setReceiptSaleId(null)} />}
    </div>
  );
}
