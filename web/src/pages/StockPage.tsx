import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, apiErrorMessage } from '../api/client';
import type { Paged, ResolvedBarcode, StockLevel, UnitType, Warehouse } from '../api/types';
import { downloadPdf } from '../lib/download';
import { inputCls, btnPrimary, btnGhost } from '../components/Modal';

interface EntryLine { key: string; variantId: string; name: string; unitType: UnitType; quantity: number; unitPurchasePrice: number; }

export function StockPage() {
  const warehouses = useQuery({ queryKey: ['warehouses'], queryFn: async () => (await api.get<Warehouse[]>('/warehouses')).data });
  const [warehouseId, setWarehouseId] = useState('');
  const [barcode, setBarcode] = useState('');
  const [lines, setLines] = useState<EntryLine[]>([]);
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  useEffect(() => {
    if (warehouses.data && !warehouseId && warehouses.data.length) {
      setWarehouseId((warehouses.data.find((w) => w.isDefault) ?? warehouses.data[0]).id);
    }
  }, [warehouses.data, warehouseId]);

  const stock = useQuery({
    queryKey: ['stock', warehouseId],
    queryFn: async () => (await api.get<Paged<StockLevel>>(`/stock/warehouse/${warehouseId}?pageSize=200`)).data,
    enabled: !!warehouseId,
  });

  const addBarcode = async (code: string) => {
    if (!code.trim()) return;
    try {
      const r = (await api.get<ResolvedBarcode>(`/variants/resolve?barcode=${encodeURIComponent(code.trim())}`)).data;
      setLines((l) => [...l, {
        key: crypto.randomUUID(), variantId: r.variantId,
        name: `${r.productName} ${r.color ?? ''} ${r.size ?? ''} (${r.unitType})`.trim(),
        unitType: r.unitType, quantity: 1, unitPurchasePrice: 0,
      }]);
      setBarcode('');
    } catch (e) { setMsg({ kind: 'err', text: apiErrorMessage(e) }); }
  };

  const submitEntry = async () => {
    setMsg(null);
    if (!warehouseId || lines.length === 0) return setMsg({ kind: 'err', text: 'Depo ve en az bir satır gerekli.' });
    try {
      await api.post('/stock/entry', {
        warehouseId, idempotencyKey: crypto.randomUUID(),
        lines: lines.map((l) => ({ variantId: l.variantId, unitType: l.unitType, quantity: l.quantity, unitPurchasePrice: l.unitPurchasePrice })),
      });
      setMsg({ kind: 'ok', text: 'Ürün girişi kaydedildi.' });
      setLines([]);
      stock.refetch();
    } catch (e) { setMsg({ kind: 'err', text: apiErrorMessage(e) }); }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Stok</h1>
        <button className={btnGhost} onClick={() => downloadPdf(`/reports/stock.pdf?warehouseId=${warehouseId}`, 'stok-raporu.pdf')}>Stok Raporu PDF</button>
      </div>
      {msg && <div className={`rounded p-2 text-sm ${msg.kind === 'ok' ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'}`}>{msg.text}</div>}

      <select className={inputCls + ' max-w-xs'} value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
        {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
      </select>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="rounded-lg bg-white p-4 shadow">
          <h2 className="mb-3 font-semibold text-slate-700">Ürün Girişi</h2>
          <input className={inputCls + ' mb-2'} placeholder="Barkod okut + Enter" value={barcode}
            onChange={(e) => setBarcode(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter') addBarcode(barcode); }} />
          <table className="mb-2 w-full text-xs">
            <thead className="text-left text-slate-500"><tr><th className="p-1">Ürün</th><th className="p-1 w-20">Miktar</th><th className="p-1 w-24">Alış</th><th></th></tr></thead>
            <tbody>
              {lines.map((l) => (
                <tr key={l.key} className="border-t">
                  <td className="p-1">{l.name}</td>
                  <td className="p-1"><input type="number" className={inputCls} value={l.quantity} onChange={(e) => setLines((ls) => ls.map((x) => x.key === l.key ? { ...x, quantity: +e.target.value } : x))} /></td>
                  <td className="p-1"><input type="number" className={inputCls} value={l.unitPurchasePrice} onChange={(e) => setLines((ls) => ls.map((x) => x.key === l.key ? { ...x, unitPurchasePrice: +e.target.value } : x))} /></td>
                  <td className="p-1 text-right"><button className="text-red-500" onClick={() => setLines((ls) => ls.filter((x) => x.key !== l.key))}>✕</button></td>
                </tr>
              ))}
              {lines.length === 0 && <tr><td colSpan={4} className="p-2 text-slate-400">Satır yok</td></tr>}
            </tbody>
          </table>
          <button className={btnPrimary + ' w-full'} onClick={submitEntry}>Girişi Kaydet</button>
        </div>

        <div className="rounded-lg bg-white p-4 shadow">
          <h2 className="mb-3 font-semibold text-slate-700">Depo Stoğu</h2>
          <table className="w-full text-xs">
            <thead className="text-left text-slate-500"><tr><th className="p-1">Ürün</th><th className="p-1">Barkod</th><th className="p-1 text-right">Miktar</th><th className="p-1 text-right">Min</th></tr></thead>
            <tbody>
              {stock.data?.items.map((s) => (
                <tr key={s.variantId} className={`border-t ${s.isBelowMin ? 'bg-red-50' : ''}`}>
                  <td className="p-1">{s.productName} {s.color} {s.size}</td>
                  <td className="p-1 font-mono">{s.adetBarcode}</td>
                  <td className="p-1 text-right font-medium">{s.quantity}</td>
                  <td className="p-1 text-right">{s.minStock}</td>
                </tr>
              ))}
              {stock.data?.items.length === 0 && <tr><td colSpan={4} className="p-2 text-slate-400">Stok yok</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
