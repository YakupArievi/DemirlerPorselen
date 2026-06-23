import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import type { Paged, Variant } from '../api/types';
import { tl } from '../lib/format';

/**
 * Aranabilir ürün/varyant seçici. Kullanıcı ürün adını yazar, listeden tıklayıp ekler.
 * Barkod gerektirmez.
 */
export function VariantPicker({ onPick, placeholder = 'Ürün adı yaz ve listeden seç...' }: {
  onPick: (v: Variant) => void;
  placeholder?: string;
}) {
  const [q, setQ] = useState('');
  const [open, setOpen] = useState(false);

  const { data } = useQuery({
    queryKey: ['variant-search', q],
    queryFn: async () => (await api.get<Paged<Variant>>(`/variants?pageSize=15&search=${encodeURIComponent(q)}`)).data,
    enabled: open,
  });

  const items = data?.items ?? [];

  return (
    <div className="relative">
      <input
        className="w-full rounded border border-slate-300 px-3 py-2"
        placeholder={placeholder}
        value={q}
        onChange={(e) => { setQ(e.target.value); setOpen(true); }}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 200)}
      />
      {open && items.length > 0 && (
        <div className="absolute z-20 mt-1 max-h-72 w-full overflow-auto rounded-lg border border-slate-200 bg-white shadow-lg">
          {items.map((v) => (
            <button
              key={v.id}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => { onPick(v); setQ(''); setOpen(false); }}
              className="flex w-full items-center justify-between gap-2 border-b px-3 py-2 text-left text-sm hover:bg-sky-50 last:border-0"
            >
              <span className="font-medium">{v.productName}</span>
              <span className="shrink-0 text-slate-600">{tl(v.salePrice)}</span>
            </button>
          ))}
        </div>
      )}
      {open && q.length > 0 && items.length === 0 && (
        <div className="absolute z-20 mt-1 w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-400 shadow-lg">
          Eşleşen ürün yok. Önce <b>Ürünler</b> sayfasından ürün ve varyant ekleyin.
        </div>
      )}
    </div>
  );
}
