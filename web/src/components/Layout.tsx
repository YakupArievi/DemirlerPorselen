import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../store/auth';
import { api } from '../api/client';

const nav = [
  { to: '/', label: 'Panel', end: true },
  { to: '/sales', label: 'Satış' },
  { to: '/products', label: 'Ürünler' },
  { to: '/stock', label: 'Stok' },
  { to: '/customers', label: 'Cari / Müşteri' },
];

export function Layout() {
  const { user, refreshToken, clear } = useAuth();
  const navigate = useNavigate();

  const logout = async () => {
    try {
      if (refreshToken) await api.post('/auth/logout', { refreshToken });
    } catch { /* yoksay */ }
    clear();
    navigate('/login');
  };

  return (
    <div className="flex h-full">
      <aside className="w-56 shrink-0 bg-slate-900 text-slate-100 flex flex-col">
        <div className="px-4 py-4 text-lg font-bold border-b border-slate-700">Toptancı</div>
        <nav className="flex-1 p-2 space-y-1">
          {nav.map((n) => (
            <NavLink
              key={n.to}
              to={n.to}
              end={n.end}
              className={({ isActive }) =>
                `block rounded px-3 py-2 text-sm ${isActive ? 'bg-sky-600 text-white' : 'hover:bg-slate-800'}`
              }
            >
              {n.label}
            </NavLink>
          ))}
        </nav>
        <div className="p-3 border-t border-slate-700 text-xs">
          <div className="font-medium">{user?.fullName}</div>
          <div className="text-slate-400">{user?.role}</div>
          <button onClick={logout} className="mt-2 w-full rounded bg-slate-700 hover:bg-slate-600 py-1.5">
            Çıkış
          </button>
        </div>
      </aside>
      <main className="flex-1 overflow-auto p-6">
        <Outlet />
      </main>
    </div>
  );
}
