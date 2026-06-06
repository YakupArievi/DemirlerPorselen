import { QRCodeSVG } from 'qrcode.react';
import type { Variant } from '../api/types';
import { tl } from '../lib/format';
import { Modal, btnPrimary, btnGhost } from './Modal';

function Label({ value, title, sub, price }: { value: string; title: string; sub: string; price: string }) {
  return (
    <div className="flex flex-col items-center rounded border border-slate-300 p-3">
      <QRCodeSVG value={value} size={150} />
      <div className="mt-2 text-center text-sm font-semibold text-slate-800">{title}</div>
      <div className="text-center text-xs text-slate-500">{sub}</div>
      <div className="mt-1 font-mono text-[11px] text-slate-500">{value}</div>
      <div className="text-sm font-bold text-slate-800">{price}</div>
    </div>
  );
}

/** Varyant için yazdırılabilir QR etiketleri (adet + koli). */
export function QrLabelModal({ variant, onClose }: { variant: Variant; onClose: () => void }) {
  const desc = [variant.color, variant.size].filter(Boolean).join(' / ');
  return (
    <Modal title="QR Etiket" onClose={onClose} wide>
      <div id="qr-print" className="flex flex-wrap justify-center gap-6">
        <Label value={variant.adetBarcode} title={variant.productName} sub={`${desc} · Adet`} price={tl(variant.salePrice)} />
        <Label value={variant.koliBarcode} title={variant.productName} sub={`${desc} · Koli (${variant.koliIciAdet})`} price={tl(variant.salePrice * variant.koliIciAdet)} />
      </div>
      <div className="mt-4 flex justify-end gap-2">
        <button className={btnGhost} onClick={onClose}>Kapat</button>
        <button className={btnPrimary} onClick={() => window.print()}>Yazdır</button>
      </div>
    </Modal>
  );
}
