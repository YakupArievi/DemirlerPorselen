import { useEffect, useRef, useState } from 'react';

export interface RowAction {
  label: string;
  onClick: () => void;
  danger?: boolean;
}

/** Satır sonundaki küçük "⋯" işlemler menüsü (Düzenle / Sil / İptal vb.). */
export function RowActions({ actions }: { actions: RowAction[] }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onDoc = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
  }, []);

  return (
    <div className="relative inline-block text-left" ref={ref}>
      <button
        type="button"
        aria-label="İşlemler"
        onClick={() => setOpen((o) => !o)}
        className="rounded px-2 py-1 text-lg leading-none text-slate-500 hover:bg-slate-100"
      >
        ⋯
      </button>
      {open && (
        <div className="absolute right-0 z-30 mt-1 w-36 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-lg">
          {actions.map((a, i) => (
            <button
              key={i}
              type="button"
              onClick={() => { setOpen(false); a.onClick(); }}
              className={`block w-full px-3 py-2 text-left text-sm hover:bg-slate-50 ${a.danger ? 'text-red-600' : 'text-slate-700'}`}
            >
              {a.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
