import { Navigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import type { Dashboard as DashboardData } from '../api/types';
import { tl, dateStr } from '../lib/format';
import { useAuth } from '../store/auth';

function Card({ title, value, accent }: { title: string; value: string; accent?: string }) {
  return (
    <div className="rounded-lg bg-white p-4 shadow">
      <div className="text-sm text-slate-500">{title}</div>
      <div className={`mt-1 text-2xl font-bold ${accent ?? 'text-slate-800'}`}>{value}</div>
    </div>
  );
}

export function Dashboard() {
  const role = useAuth((s) => s.user?.role);
  if (role === 'Depocu') return <Navigate to="/sales" replace />;
  return <DashboardInner />;
}

function DashboardInner() {
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await api.get<DashboardData>('/dashboard/summary')).data,
  });

  if (isLoading || !data) return <div>Yükleniyor...</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-slate-800">Panel</h1>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Card title="Bugün Ciro" value={tl(data.todayRevenue)} accent="text-sky-600" />
        <Card title="Bu Ay Ciro" value={tl(data.monthRevenue)} accent="text-sky-600" />
        <Card title="Bugün Tahsilat" value={tl(data.todayCollections)} accent="text-emerald-600" />
        <Card title="Toplam Alacak" value={tl(data.totalReceivables)} accent="text-amber-600" />
      </div>
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Card title="Kritik Stok" value={`${data.criticalStockCount} ürün`} accent="text-red-600" />
        <Card title="Bu Ay Kırık" value={`${data.brokenQuantityThisMonth} adet`} accent="text-red-500" />
        <Card title="Bu Ay Tahsilat" value={tl(data.monthCollections)} accent="text-emerald-600" />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="rounded-lg bg-white p-4 shadow lg:col-span-1">
          <h2 className="mb-3 font-semibold text-slate-700">Kritik Stoklar</h2>
          {data.criticalStocks.length === 0 ? <p className="text-sm text-slate-400">Yok</p> : (
            <ul className="space-y-1 text-sm">
              {data.criticalStocks.map((c) => (
                <li key={c.variantId} className="flex justify-between">
                  <span>{c.productName} {c.color} {c.size}</span>
                  <span className="text-red-600">{c.totalQuantity}/{c.minStock}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg bg-white p-4 shadow lg:col-span-1">
          <h2 className="mb-3 font-semibold text-slate-700">En Çok Satanlar (Bu Ay)</h2>
          {data.topProducts.length === 0 ? <p className="text-sm text-slate-400">Veri yok</p> : (
            <ul className="space-y-1 text-sm">
              {data.topProducts.map((p) => (
                <li key={p.variantId} className="flex justify-between">
                  <span>{p.productName} {p.color} {p.size}</span>
                  <span className="text-slate-600">{p.soldQuantity} adet · {tl(p.revenue)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg bg-white p-4 shadow lg:col-span-1">
          <h2 className="mb-3 font-semibold text-slate-700">Son Satışlar</h2>
          {data.recentSales.length === 0 ? <p className="text-sm text-slate-400">Veri yok</p> : (
            <ul className="space-y-1 text-sm">
              {data.recentSales.map((s) => (
                <li key={s.id} className="flex justify-between">
                  <span>#{s.saleNumber} {s.customerName}</span>
                  <span className="text-slate-600">{dateStr(s.saleDate)} · {tl(s.grandTotal)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
