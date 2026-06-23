import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

export interface RowAction {
  label: string;
  onClick: () => void;
  danger?: boolean;
}

/**
 * Satır sonundaki küçük "⋯" işlemler menüsü (Düzenle / Sil / İptal vb.).
 * Menü body'ye portal ile, fixed konumda açılır — tablo/kart `overflow-hidden`
 * olsa bile kırpılmaz (en alttaki satırlarda da görünür).
 */
export function RowActions({ actions }: { actions: RowAction[] }) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (menuRef.current?.contains(e.target as Node) || btnRef.current?.contains(e.target as Node)) return;
      setOpen(false);
    };
    const onScroll = () => setOpen(false);
    document.addEventListener('mousedown', onDoc);
    window.addEventListener('scroll', onScroll, true);
    return () => { document.removeEventListener('mousedown', onDoc); window.removeEventListener('scroll', onScroll, true); };
  }, [open]);

  const toggle = () => {
    if (!open && btnRef.current) {
      const r = btnRef.current.getBoundingClientRect();
      const menuW = 150;
      const menuH = actions.length * 38 + 8;
      let top = r.bottom + 4;
      if (top + menuH > window.innerHeight) top = Math.max(8, r.top - menuH - 4); // aşağı sığmıyorsa yukarı aç
      setPos({ top, left: Math.max(8, r.right - menuW) });
    }
    setOpen((o) => !o);
  };

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        aria-label="İşlemler"
        onClick={toggle}
        className="rounded px-2 py-1 text-lg leading-none text-slate-500 hover:bg-slate-100"
      >
        ⋯
      </button>
      {open && pos && createPortal(
        <div
          ref={menuRef}
          style={{ position: 'fixed', top: pos.top, left: pos.left, width: 150 }}
          className="z-[100] overflow-hidden rounded-lg border border-slate-200 bg-white shadow-lg"
        >
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
        </div>,
        document.body,
      )}
    </>
  );
}
