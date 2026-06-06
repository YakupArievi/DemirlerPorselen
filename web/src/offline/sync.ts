import axios from 'axios';
import { api } from '../api/client';
import type { Customer, Paged, ResolvedBarcode, Variant, Warehouse } from '../api/types';
import { db, setMeta } from './db';

/** Online iken master veriyi çekip yerelde saklar. */
export async function cacheMasterData(): Promise<void> {
  const variants: Variant[] = [];
  let page = 1;
  for (;;) {
    const res = await api.get<Paged<Variant>>(`/variants?page=${page}&pageSize=200`);
    variants.push(...res.data.items);
    if (page >= res.data.totalPages || res.data.items.length === 0) break;
    page++;
  }
  const customers = (await api.get<Paged<Customer>>('/customers?pageSize=1000')).data.items;
  const warehouses = (await api.get<Warehouse[]>('/warehouses')).data;

  await db.transaction('rw', db.variants, db.customers, db.warehouses, async () => {
    await db.variants.clear(); await db.variants.bulkPut(variants);
    await db.customers.clear(); await db.customers.bulkPut(customers);
    await db.warehouses.clear(); await db.warehouses.bulkPut(warehouses);
  });
  await setMeta('lastSync', new Date().toISOString());
}

/** Barkodu önce yerel cache'ten çözer (offline destekli). */
export async function resolveBarcodeLocal(code: string): Promise<ResolvedBarcode | null> {
  const c = code.trim();
  let v = await db.variants.where('adetBarcode').equals(c).first();
  let unit: 'Adet' | 'Koli' = 'Adet';
  if (!v) { v = await db.variants.where('koliBarcode').equals(c).first(); unit = 'Koli'; }
  if (!v) return null;
  return {
    variantId: v.id, productName: v.productName, color: v.color, pattern: v.pattern, size: v.size,
    unitType: unit, adetEquivalent: unit === 'Koli' ? v.koliIciAdet : 1, salePrice: v.salePrice,
  };
}

/** İşlemi offline kuyruğa yazar. */
export async function enqueue(type: 'sale' | 'payment', endpoint: string, payload: unknown, id: string) {
  await db.queue.put({ id, type, endpoint, payload, status: 'pending', createdAt: Date.now(), attempts: 0 });
}

export async function pendingCount(): Promise<number> {
  return db.queue.where('status').anyOf('pending', 'failed').count();
}

let syncing = false;

/**
 * Kuyruğu SIRAYLA sunucuya gönderir (satış→iptal bağımlılığı için FIFO).
 * Sunucu idempotencyKey ile dedupe eder; başarılı kayıt silinir.
 * Ağ hatasında durur (sonra tekrar denenir); iş kuralı (4xx) hatasında 'failed' işaretlenir.
 */
export async function syncQueue(): Promise<{ synced: number; failed: number }> {
  if (syncing || !navigator.onLine) return { synced: 0, failed: 0 };
  syncing = true;
  let synced = 0, failed = 0;
  try {
    const items = await db.queue.where('status').anyOf('pending', 'failed').sortBy('createdAt');
    for (const op of items) {
      await db.queue.update(op.id, { status: 'syncing', attempts: op.attempts + 1 });
      try {
        await api.post(op.endpoint, op.payload);
        await db.queue.delete(op.id); // sunucu idempotencyKey ile dedupe ettiği için güvenli
        synced++;
      } catch (e) {
        if (axios.isAxiosError(e) && e.response) {
          // İş kuralı hatası (eksi stok hariç çoğu 4xx) -> işaretle, devam et
          const msg = (e.response.data as { message?: string })?.message ?? `HTTP ${e.response.status}`;
          await db.queue.update(op.id, { status: 'failed', error: msg });
          failed++;
        } else {
          // Ağ hatası -> beklet, kuyruğu durdur (sıra korunsun)
          await db.queue.update(op.id, { status: 'pending' });
          break;
        }
      }
    }
  } finally {
    syncing = false;
  }
  return { synced, failed };
}
