import { Fragment, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiErrorMessage } from '../api/client';
import type { Brand, Category, Paged, Product, Variant } from '../api/types';
import { Modal, Field, inputCls, btnPrimary, btnGhost } from '../components/Modal';
import { QrLabelModal } from '../components/QrLabel';
import { tl } from '../lib/format';

export function Products() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [showProduct, setShowProduct] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);

  const categories = useQuery({ queryKey: ['categories'], queryFn: async () => (await api.get<Paged<Category>>('/categories?pageSize=200')).data });
  const brands = useQuery({ queryKey: ['brands'], queryFn: async () => (await api.get<Paged<Brand>>('/brands?pageSize=200')).data });
  const products = useQuery({
    queryKey: ['products', search],
    queryFn: async () => (await api.get<Paged<Product>>(`/products?pageSize=100&search=${encodeURIComponent(search)}`)).data,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Ürünler</h1>
        <button className={btnPrimary} onClick={() => setShowProduct(true)}>Yeni Ürün</button>
      </div>

      <input className={inputCls + ' max-w-sm'} placeholder="Ürün ara..." value={search} onChange={(e) => setSearch(e.target.value)} />

      <div className="overflow-hidden rounded-lg bg-white shadow">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-slate-500">
            <tr><th className="p-3">Ürün</th><th className="p-3">Kategori</th><th className="p-3">Marka</th><th className="p-3">Varyant</th><th></th></tr>
          </thead>
          <tbody>
            {products.data?.items.map((p) => (
              <Fragment key={p.id}>
                <tr className="border-t hover:bg-slate-50">
                  <td className="p-3 font-medium">{p.name}</td>
                  <td className="p-3">{p.categoryName}</td>
                  <td className="p-3">{p.brandName ?? '-'}</td>
                  <td className="p-3">{p.variantCount}</td>
                  <td className="p-3 text-right">
                    <button className={btnGhost} onClick={() => setExpanded(expanded === p.id ? null : p.id)}>
                      {expanded === p.id ? 'Gizle' : 'Varyantlar'}
                    </button>
                  </td>
                </tr>
                {expanded === p.id && (
                  <tr>
                    <td colSpan={5} className="bg-slate-50 p-3">
                      <VariantPanel productId={p.id} onChanged={() => qc.invalidateQueries({ queryKey: ['products'] })} />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
            {products.data?.items.length === 0 && <tr><td colSpan={5} className="p-6 text-center text-slate-400">Ürün yok</td></tr>}
          </tbody>
        </table>
      </div>

      {showProduct && (
        <ProductModal
          categories={categories.data?.items ?? []}
          brands={brands.data?.items ?? []}
          onClose={() => setShowProduct(false)}
          onSaved={() => { setShowProduct(false); qc.invalidateQueries({ queryKey: ['products'] }); }}
          onCategoryAdded={() => categories.refetch()}
        />
      )}
    </div>
  );
}

function ProductModal({ categories, brands, onClose, onSaved, onCategoryAdded }: {
  categories: Category[]; brands: Brand[]; onClose: () => void; onSaved: () => void; onCategoryAdded: () => void;
}) {
  const [name, setName] = useState('');
  const [categoryId, setCategoryId] = useState(categories[0]?.id ?? '');
  const [brandId, setBrandId] = useState('');
  const [salePrice, setSalePrice] = useState(0);
  const [purchasePrice, setPurchasePrice] = useState(0);
  const [newCat, setNewCat] = useState('');
  const [error, setError] = useState('');

  const addCategory = useMutation({
    mutationFn: async () => (await api.post('/categories', { name: newCat })).data as Category,
    onSuccess: (c) => { setNewCat(''); setCategoryId(c.id); onCategoryAdded(); },
  });

  // Ürünü oluştur + aynı anda varyantını oluştur (kullanıcı ayrıca "Varyantlar"a tıklamak zorunda değil).
  const save = useMutation({
    mutationFn: async () => {
      const p = (await api.post('/products', { name, categoryId, brandId: brandId || null })).data as { id: string };
      await api.post('/variants', { productId: p.id, koliIciAdet: 1, purchasePrice, salePrice, minStock: 0 });
    },
    onSuccess: onSaved,
    onError: (e) => setError(apiErrorMessage(e)),
  });

  return (
    <Modal title="Yeni Ürün" onClose={onClose}>
      {error && <div className="mb-3 rounded bg-red-50 p-2 text-sm text-red-600">{error}</div>}
      <Field label="Ürün adı"><input className={inputCls} value={name} onChange={(e) => setName(e.target.value)} /></Field>
      <Field label="Kategori">
        <div className="flex gap-2">
          <select className={inputCls} value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">Seçin</option>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div className="mt-2 flex gap-2">
          <input className={inputCls} placeholder="Yeni kategori" value={newCat} onChange={(e) => setNewCat(e.target.value)} />
          <button className={btnGhost} disabled={!newCat} onClick={() => addCategory.mutate()}>Ekle</button>
        </div>
      </Field>
      <Field label="Marka (opsiyonel)">
        <select className={inputCls} value={brandId} onChange={(e) => setBrandId(e.target.value)}>
          <option value="">Yok</option>
          {brands.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
      </Field>
      <Field label="Satış fiyatı">
        <input className={inputCls} type="number" value={salePrice === 0 ? '' : salePrice}
          onChange={(e) => setSalePrice(e.target.value === '' ? 0 : +e.target.value)} />
      </Field>
      <Field label="Alış fiyatı (ops.)">
        <input className={inputCls} type="number" value={purchasePrice === 0 ? '' : purchasePrice}
          onChange={(e) => setPurchasePrice(e.target.value === '' ? 0 : +e.target.value)} />
      </Field>
      <div className="flex justify-end gap-2">
        <button className={btnGhost} onClick={onClose}>İptal</button>
        <button className={btnPrimary} disabled={!name || !categoryId || !(salePrice > 0) || save.isPending} onClick={() => save.mutate()}>Kaydet</button>
      </div>
    </Modal>
  );
}

function VariantPanel({ productId, onChanged }: { productId: string; onChanged: () => void }) {
  const variants = useQuery({
    queryKey: ['variants', productId],
    queryFn: async () => (await api.get<Variant[]>(`/products/${productId}/variants`)).data,
  });
  const [form, setForm] = useState({ purchasePrice: 0, salePrice: 0 });
  const [qr, setQr] = useState<Variant | null>(null);
  const add = useMutation({
    mutationFn: async () => api.post('/variants', { productId, koliIciAdet: 1, minStock: 0, ...form }),
    onSuccess: () => { variants.refetch(); onChanged(); setForm({ purchasePrice: 0, salePrice: 0 }); },
  });

  const ic = 'flex-1 rounded border border-slate-300 px-3 py-2';
  const row = (label: string, input: React.ReactNode) => (
    <div className="flex items-center gap-3">
      <label className="w-28 shrink-0 text-sm text-slate-600">{label}</label>
      {input}
    </div>
  );

  return (
    <div>
      <table className="mb-3 w-full text-xs">
        <thead className="text-left text-slate-500">
          <tr><th className="p-1">Adet QR</th><th className="p-1">Koli QR</th><th className="p-1">Koli İçi</th><th className="p-1">Alış</th><th className="p-1">Satış</th><th></th></tr>
        </thead>
        <tbody>
          {variants.data?.map((v) => (
            <tr key={v.id} className="border-t">
              <td className="p-1 font-mono">{v.adetBarcode}</td>
              <td className="p-1 font-mono">{v.koliBarcode}</td>
              <td className="p-1">{v.koliIciAdet}</td>
              <td className="p-1">{tl(v.purchasePrice)}</td>
              <td className="p-1">{tl(v.salePrice)}</td>
              <td className="p-1 text-right"><button className={btnGhost} onClick={() => setQr(v)}>QR</button></td>
            </tr>
          ))}
          {variants.data?.length === 0 && <tr><td colSpan={6} className="p-2 text-slate-400">Varyant yok</td></tr>}
        </tbody>
      </table>
      {qr && <QrLabelModal variant={qr} onClose={() => setQr(null)} />}
      <div className="max-w-sm space-y-2">
        {row('Satış fiyatı', <input className={ic} type="number" value={form.salePrice === 0 ? '' : form.salePrice} onChange={(e) => setForm({ ...form, salePrice: e.target.value === '' ? 0 : +e.target.value })} />)}
        {row('Alış fiyatı (ops.)', <input className={ic} type="number" value={form.purchasePrice === 0 ? '' : form.purchasePrice} onChange={(e) => setForm({ ...form, purchasePrice: e.target.value === '' ? 0 : +e.target.value })} />)}
        <button className={btnPrimary} disabled={!(form.salePrice > 0) || add.isPending} onClick={() => add.mutate()}>Varyant Ekle</button>
      </div>
    </div>
  );
}
