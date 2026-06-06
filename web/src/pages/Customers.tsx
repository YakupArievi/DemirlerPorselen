import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiErrorMessage } from '../api/client';
import type { Customer, Paged } from '../api/types';
import { Modal, Field, inputCls, btnPrimary, btnGhost } from '../components/Modal';
import { tl } from '../lib/format';

export function Customers() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [show, setShow] = useState(false);

  const customers = useQuery({
    queryKey: ['customers', search],
    queryFn: async () => (await api.get<Paged<Customer>>(`/customers?pageSize=100&search=${encodeURIComponent(search)}`)).data,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Cari / Müşteri</h1>
        <button className={btnPrimary} onClick={() => setShow(true)}>Yeni Müşteri</button>
      </div>
      <input className={inputCls + ' max-w-sm'} placeholder="Müşteri ara..." value={search} onChange={(e) => setSearch(e.target.value)} />

      <div className="overflow-hidden rounded-lg bg-white shadow">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-slate-500">
            <tr><th className="p-3">Ad</th><th className="p-3">Telefon</th><th className="p-3 text-right">Bakiye</th><th></th></tr>
          </thead>
          <tbody>
            {customers.data?.items.map((c) => (
              <tr key={c.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-medium">{c.name}</td>
                <td className="p-3">{c.phone ?? '-'}</td>
                <td className={`p-3 text-right font-medium ${c.balance > 0 ? 'text-amber-600' : c.balance < 0 ? 'text-emerald-600' : ''}`}>{tl(c.balance)}</td>
                <td className="p-3 text-right"><Link className={btnGhost} to={`/customers/${c.id}`}>Detay</Link></td>
              </tr>
            ))}
            {customers.data?.items.length === 0 && <tr><td colSpan={4} className="p-6 text-center text-slate-400">Müşteri yok</td></tr>}
          </tbody>
        </table>
      </div>

      {show && <CustomerModal onClose={() => setShow(false)} onSaved={() => { setShow(false); qc.invalidateQueries({ queryKey: ['customers'] }); }} />}
    </div>
  );
}

function CustomerModal({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({ name: '', phone: '', address: '', taxNumber: '', notes: '', openingBalance: 0 });
  const [error, setError] = useState('');
  const save = useMutation({
    mutationFn: async () => api.post('/customers', form),
    onSuccess: onSaved,
    onError: (e) => setError(apiErrorMessage(e)),
  });
  return (
    <Modal title="Yeni Müşteri" onClose={onClose}>
      {error && <div className="mb-3 rounded bg-red-50 p-2 text-sm text-red-600">{error}</div>}
      <Field label="Ad / Firma"><input className={inputCls} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
      <Field label="Telefon"><input className={inputCls} value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Field>
      <Field label="Adres"><input className={inputCls} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} /></Field>
      <Field label="Vergi No"><input className={inputCls} value={form.taxNumber} onChange={(e) => setForm({ ...form, taxNumber: e.target.value })} /></Field>
      <Field label="Açılış Bakiyesi"><input type="number" className={inputCls} value={form.openingBalance} onChange={(e) => setForm({ ...form, openingBalance: +e.target.value })} /></Field>
      <div className="flex justify-end gap-2">
        <button className={btnGhost} onClick={onClose}>İptal</button>
        <button className={btnPrimary} disabled={!form.name || save.isPending} onClick={() => save.mutate()}>Kaydet</button>
      </div>
    </Modal>
  );
}
