import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, apiErrorMessage } from '../api/client';
import { useAuth } from '../store/auth';

export function Login() {
  const [userName, setUserName] = useState('admin');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const setAuth = useAuth((s) => s.setAuth);
  const navigate = useNavigate();

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await api.post('/auth/login', { userName, password });
      setAuth(res.data);
      navigate('/');
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex h-full items-center justify-center">
      <form onSubmit={submit} className="w-80 rounded-lg bg-white p-6 shadow">
        <div className="mb-4 flex flex-col items-center">
          <img src="/logo.png" alt="Demirler Porselen" className="mb-2 w-44 rounded-lg" />
          <p className="text-sm text-slate-500">Stok & Cari Yönetim</p>
        </div>
        {error && <div className="mb-3 rounded bg-red-50 p-2 text-sm text-red-600">{error}</div>}
        <label className="mb-1 block text-sm text-slate-600">Kullanıcı adı</label>
        <input value={userName} onChange={(e) => setUserName(e.target.value)}
          className="mb-3 w-full rounded border border-slate-300 px-3 py-2" />
        <label className="mb-1 block text-sm text-slate-600">Parola</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)}
          className="mb-4 w-full rounded border border-slate-300 px-3 py-2" />
        <button disabled={loading}
          className="w-full rounded bg-sky-600 py-2 font-medium text-white hover:bg-sky-700 disabled:opacity-50">
          {loading ? 'Giriş yapılıyor...' : 'Giriş'}
        </button>
      </form>
    </div>
  );
}
