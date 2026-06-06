import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiErrorMessage } from '../api/client';
import { Modal, Field, inputCls, btnPrimary, btnGhost } from '../components/Modal';

interface AppUser { id: string; userName: string; fullName: string; role: string; isActive: boolean; }
const ROLES = ['Admin', 'Patron', 'Depocu'];

export function Users() {
  const qc = useQueryClient();
  const [show, setShow] = useState(false);
  const [resetFor, setResetFor] = useState<AppUser | null>(null);

  const users = useQuery({ queryKey: ['users'], queryFn: async () => (await api.get<AppUser[]>('/users')).data });

  const toggle = useMutation({
    mutationFn: async (u: AppUser) => api.put(`/users/${u.id}`, { fullName: u.fullName, role: u.role, isActive: !u.isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['users'] }),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Kullanıcılar (Personel)</h1>
        <button className={btnPrimary} onClick={() => setShow(true)}>Yeni Kullanıcı</button>
      </div>

      <div className="overflow-hidden rounded-lg bg-white shadow">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-slate-500">
            <tr><th className="p-3">Kullanıcı adı</th><th className="p-3">Ad</th><th className="p-3">Rol</th><th className="p-3">Durum</th><th></th></tr>
          </thead>
          <tbody>
            {users.data?.map((u) => (
              <tr key={u.id} className="border-t">
                <td className="p-3 font-medium">{u.userName}</td>
                <td className="p-3">{u.fullName}</td>
                <td className="p-3">{u.role}</td>
                <td className="p-3">
                  <span className={`rounded px-2 py-0.5 text-xs ${u.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-600'}`}>
                    {u.isActive ? 'Aktif' : 'Pasif'}
                  </span>
                </td>
                <td className="p-3 text-right space-x-2">
                  <button className={btnGhost} onClick={() => setResetFor(u)}>Parola</button>
                  <button className={btnGhost} onClick={() => toggle.mutate(u)}>{u.isActive ? 'Pasifleştir' : 'Aktifleştir'}</button>
                </td>
              </tr>
            ))}
            {users.data?.length === 0 && <tr><td colSpan={5} className="p-6 text-center text-slate-400">Kullanıcı yok</td></tr>}
          </tbody>
        </table>
      </div>

      {show && <CreateUserModal onClose={() => setShow(false)} onSaved={() => { setShow(false); qc.invalidateQueries({ queryKey: ['users'] }); }} />}
      {resetFor && <ResetPasswordModal user={resetFor} onClose={() => setResetFor(null)} />}
    </div>
  );
}

function CreateUserModal({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({ userName: '', fullName: '', role: 'Depocu', password: '' });
  const [error, setError] = useState('');
  const save = useMutation({
    mutationFn: async () => api.post('/users', form),
    onSuccess: onSaved,
    onError: (e) => setError(apiErrorMessage(e)),
  });
  return (
    <Modal title="Yeni Kullanıcı" onClose={onClose}>
      {error && <div className="mb-3 rounded bg-red-50 p-2 text-sm text-red-600">{error}</div>}
      <Field label="Kullanıcı adı"><input className={inputCls} value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} /></Field>
      <Field label="Ad Soyad"><input className={inputCls} value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></Field>
      <Field label="Rol">
        <select className={inputCls} value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}>
          {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
        </select>
      </Field>
      <Field label="Parola"><input type="password" className={inputCls} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></Field>
      <div className="flex justify-end gap-2">
        <button className={btnGhost} onClick={onClose}>İptal</button>
        <button className={btnPrimary} disabled={!form.userName || !form.fullName || !form.password || save.isPending} onClick={() => save.mutate()}>Kaydet</button>
      </div>
    </Modal>
  );
}

function ResetPasswordModal({ user, onClose }: { user: AppUser; onClose: () => void }) {
  const [pw, setPw] = useState('');
  const [msg, setMsg] = useState('');
  const save = useMutation({
    mutationFn: async () => api.post(`/users/${user.id}/reset-password`, { password: pw }),
    onSuccess: () => { setMsg('Parola güncellendi.'); setPw(''); },
    onError: (e) => setMsg(apiErrorMessage(e)),
  });
  return (
    <Modal title={`Parola: ${user.userName}`} onClose={onClose}>
      {msg && <div className="mb-3 rounded bg-slate-100 p-2 text-sm">{msg}</div>}
      <Field label="Yeni parola"><input type="password" className={inputCls} value={pw} onChange={(e) => setPw(e.target.value)} /></Field>
      <div className="flex justify-end gap-2">
        <button className={btnGhost} onClick={onClose}>Kapat</button>
        <button className={btnPrimary} disabled={!pw || save.isPending} onClick={() => save.mutate()}>Güncelle</button>
      </div>
    </Modal>
  );
}
