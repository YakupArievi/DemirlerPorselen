import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import type { Sale, Statement } from '../api/types';
import { tl, dateStr } from '../lib/format';
import { Modal, btnPrimary, btnGhost } from './Modal';

/** Bir satışa ait yazdırılabilir fiş: ürünler, adet, birim fiyat, tutar + önceki/sonraki bakiye. */
export function SaleReceiptModal({ saleId, onClose }: { saleId: string; onClose: () => void }) {
  const sale = useQuery({ queryKey: ['sale', saleId], queryFn: async () => (await api.get<Sale>(`/sales/${saleId}`)).data });
  const customerId = sale.data?.customerId;
  const statement = useQuery({
    queryKey: ['statement', customerId],
    enabled: !!customerId,
    queryFn: async () => (await api.get<Statement>(`/customers/${customerId}/statement`)).data,
  });

  const s = sale.data;
  let before: number | null = null, after: number | null = null;
  if (s && statement.data) {
    const line = statement.data.lines.find((l) => l.referenceType === 'Sale' && l.referenceId === saleId);
    if (line) { after = line.runningBalance; before = line.runningBalance - (line.debit - line.credit); }
  }
  const balLabel = (b: number) => (b > 0 ? 'Borçlu' : b < 0 ? 'Alacaklı' : 'Bakiye sıfır');

  return (
    <Modal title={`Satış Fişi${s ? ' #' + s.saleNumber : ''}`} onClose={onClose}>
      {!s ? <div className="p-4 text-slate-500">Yükleniyor...</div> : (
        <>
          <div id="receipt-print" className="mx-auto bg-white p-5 text-sm text-slate-800" style={{ width: 360 }}>
            <div className="mb-3 text-center">
              <div className="text-lg font-bold">Demirler Porselen</div>
              <div className="text-xs text-slate-500">Satış Fişi</div>
            </div>
            <div className="mb-1 flex justify-between text-xs"><span>Fiş No: #{s.saleNumber}</span><span>{dateStr(s.saleDate)}</span></div>
            <div className="mb-3 text-xs">Müşteri: <b>{s.customerName}</b>{s.status === 'Cancelled' && <span className="text-red-500"> (İPTAL)</span>}</div>

            <table className="w-full text-xs">
              <thead>
                <tr className="border-b text-left text-slate-500">
                  <th className="py-1">Ürün</th><th className="py-1 text-right">Adet</th><th className="py-1 text-right">B.Fiyat</th><th className="py-1 text-right">Tutar</th>
                </tr>
              </thead>
              <tbody>
                {s.lines.map((l) => (
                  <tr key={l.id} className="border-b border-dashed border-slate-200">
                    <td className="py-1">{l.productName}</td>
                    <td className="py-1 text-right">{l.quantity} {l.unitType === 'Koli' ? 'koli' : 'ad'}</td>
                    <td className="py-1 text-right">{tl(l.unitPrice)}</td>
                    <td className="py-1 text-right">{tl(l.lineTotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="mt-2 space-y-0.5 text-xs">
              <div className="flex justify-between"><span>Ara Toplam</span><span>{tl(s.subTotal)}</span></div>
              {s.discountTotal > 0 && <div className="flex justify-between"><span>İskonto</span><span>-{tl(s.discountTotal)}</span></div>}
              <div className="flex justify-between text-sm font-bold"><span>Genel Toplam</span><span>{tl(s.grandTotal)}</span></div>
              <div className="flex justify-between"><span>Ödenen</span><span>{tl(s.paidAmount)}</span></div>
            </div>

            {before !== null && after !== null ? (
              <div className="mt-3 border-t pt-2 text-xs">
                <div className="flex justify-between"><span>Önceki bakiye</span><span>{tl(before)} ({balLabel(before)})</span></div>
                <div className="flex justify-between font-bold"><span>Satış sonrası bakiye</span><span>{tl(after)} ({balLabel(after)})</span></div>
              </div>
            ) : (
              <div className="mt-3 border-t pt-2 text-[11px] text-slate-400">Bakiye bilgisi bu fiş için bulunamadı (eski tarihli olabilir).</div>
            )}

            <div className="mt-4 text-center text-[10px] text-slate-400">Teşekkür ederiz</div>
          </div>

          <div className="mt-4 flex justify-end gap-2">
            <button className={btnGhost} onClick={onClose}>Kapat</button>
            <button className={btnPrimary} onClick={() => window.print()}>Yazdır</button>
          </div>
        </>
      )}
    </Modal>
  );
}
