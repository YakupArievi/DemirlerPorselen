import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiErrorMessage } from '../api/client';
import type { Customer, Statement } from '../api/types';
import { tl, dateStr } from '../lib/format';
import { downloadPdf } from '../lib/download';
import { inputCls, btnPrimary, btnGhost } from '../components/Modal';

export function CustomerDetail() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const [payType, setPayType] = useState<'Nakit' | 'Kart' | 'Cek'>('Nakit');
  const [payAmount, setPayAmount] = useState(0);
  const [dueDate, setDueDate] = useState('');
  const [msg, setMsg] = useState('');

  const customer = useQuery({ queryKey: ['customer', id], queryFn: async () => (await api.get<Customer>(`/customers/${id}`)).data });
  const statement = useQuery({ queryKey: ['statement', id], queryFn: async () => (await api.get<Statement>(`/customers/${id}/statement`)).data });

  const pay = useMutation({
    mutationFn: async () => api.post('/payments', {
      customerId: id, type: payType, amount: payAmount,
      dueDate: payType === 'Cek' && dueDate ? dueDate : null, idempotencyKey: crypto.randomUUID(),
    }),
    onSuccess: () => {
      setMsg('Tahsilat kaydedildi.'); setPayAmount(0);
      qc.invalidateQueries({ queryKey: ['customer', id] });
      qc.invalidateQueries({ queryKey: ['statement', id] });
    },
    onError: (e) => setMsg(apiErrorMessage(e)),
  });

  if (!customer.data) return <div>Yükleniyor...</div>;
  const c = customer.data;

  return (
    <div className="space-y-4">
      <Link to="/customers" className="text-sm text-sky-600">← Müşteriler</Link>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">{c.name}</h1>
          <p className="text-sm text-slate-500">{c.phone}</p>
        </div>
        <div className="text-right">
          <div className="text-sm text-slate-500">Bakiye</div>
          <div className={`text-2xl font-bold ${c.balance > 0 ? 'text-amber-600' : 'text-emerald-600'}`}>{tl(c.balance)}</div>
        </div>
      </div>

      {msg && <div className="rounded bg-slate-100 p-2 text-sm">{msg}</div>}

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="rounded-lg bg-white p-4 shadow">
          <h2 className="mb-3 font-semibold text-slate-700">Tahsilat Girişi</h2>
          <select className={inputCls + ' mb-2'} value={payType} onChange={(e) => setPayType(e.target.value as typeof payType)}>
            <option value="Nakit">Nakit</option><option value="Kart">Kart</option><option value="Cek">Çek</option>
          </select>
          <input type="number" className={inputCls + ' mb-2'} placeholder="Tutar" value={payAmount} onChange={(e) => setPayAmount(+e.target.value)} />
          {payType === 'Cek' && <input type="date" className={inputCls + ' mb-2'} value={dueDate} onChange={(e) => setDueDate(e.target.value)} />}
          <button className={btnPrimary + ' w-full'} disabled={payAmount <= 0 || pay.isPending} onClick={() => pay.mutate()}>Tahsilat Ekle</button>
        </div>

        <div className="rounded-lg bg-white p-4 shadow lg:col-span-2">
          <h2 className="mb-3 font-semibold text-slate-700">Raporlar</h2>
          <div className="flex gap-2">
            <button className={btnGhost} onClick={() => downloadPdf(`/reports/customers/${id}/statement.pdf`, `ekstre-${c.name}.pdf`)}>Cari Ekstre PDF</button>
            <button className={btnGhost} onClick={() => downloadPdf(`/reports/customers/${id}/summary.pdf`, `ozet-${c.name}.pdf`)}>Müşteri Özeti PDF</button>
          </div>
        </div>
      </div>

      <div className="rounded-lg bg-white p-4 shadow">
        <h2 className="mb-3 font-semibold text-slate-700">Ekstre (son 1 ay)</h2>
        <table className="w-full text-sm">
          <thead className="text-left text-slate-500">
            <tr><th className="p-2">Tarih</th><th className="p-2">İşlem</th><th className="p-2 text-right">Borç</th><th className="p-2 text-right">Alacak</th><th className="p-2 text-right">Bakiye</th></tr>
          </thead>
          <tbody>
            <tr className="border-t text-slate-500"><td className="p-2" colSpan={4}>Açılış Bakiyesi</td><td className="p-2 text-right">{tl(statement.data?.openingBalance ?? 0)}</td></tr>
            {statement.data?.lines.map((l, idx) => (
              <tr key={idx} className="border-t">
                <td className="p-2">{dateStr(l.date)}</td>
                <td className="p-2">{l.type}</td>
                <td className="p-2 text-right">{l.debit ? tl(l.debit) : ''}</td>
                <td className="p-2 text-right">{l.credit ? tl(l.credit) : ''}</td>
                <td className="p-2 text-right">{tl(l.runningBalance)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
