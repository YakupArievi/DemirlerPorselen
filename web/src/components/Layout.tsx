import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../store/auth';
import { api } from '../api/client';
import { useSyncEngine } from '../offline/useSyncEngine';
import { useOffline } from '../offline/store';

type Role = 'Admin' | 'Patron' | 'Depocu';
const nav: { to: string; label: string; end?: boolean; roles: Role[] }[] = [
  { to: '/', label: 'Panel', end: true, roles: ['Admin', 'Patron'] },
  { to: '/sales', label: 'Satış', roles: ['Admin', 'Patron', 'Depocu'] },
  { to: '/products', label: 'Ürünler', roles: ['Admin', 'Patron', 'Depocu'] },
  { to: '/stock', label: 'Stok', roles: ['Admin', 'Patron', 'Depocu'] },
  { to: '/customers', label: 'Cari / Müşteri', roles: ['Admin', 'Patron'] },
  { to: '/users', label: 'Kullanıcılar', roles: ['Admin'] },
];

export function Layout() {
  const { user, refreshToken, clear } = useAuth();
  const role = (user?.role ?? 'Depocu') as Role;
  const visibleNav = nav.filter((n) => n.roles.includes(role));
  const navigate = useNavigate();
  useSyncEngine();
  const { online, pending } = useOffline();

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
        <div className="flex items-center gap-2 px-4 py-4 border-b border-slate-700">
          <img src="/favicon.svg" alt="" className="h-8 w-8 rounded-lg" />
          <span className="text-base font-bold leading-tight">Demirler<br /><span className="text-xs font-medium text-slate-400">Porselen</span></span>
        </div>
        <nav className="flex-1 p-2 space-y-1">
          {visibleNav.map((n) => (
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
          <div className="mb-2 flex items-center gap-2">
            <span className={`inline-block h-2 w-2 rounded-full ${online ? 'bg-emerald-400' : 'bg-amber-400'}`} />
            <span>{online ? 'Çevrimiçi' : 'Çevrimdışı'}</span>
            {pending > 0 && <span className="ml-auto rounded bg-amber-500 px-1.5 text-[10px] text-white">{pending} bekliyor</span>}
          </div>
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
