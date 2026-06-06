import Dexie, { type Table } from 'dexie';
import type { Customer, Variant, Warehouse } from '../api/types';

export type QueueStatus = 'pending' | 'syncing' | 'failed';

/** Offline kuyruğa yazılan işlem. Client-üretimi id + idempotencyKey ile. */
export interface QueuedOperation {
  id: string;            // client uuid (idempotencyKey olarak da kullanılır)
  type: 'sale' | 'payment';
  endpoint: string;      // örn. '/sales'
  payload: unknown;      // gövde (idempotencyKey içerir)
  status: QueueStatus;
  createdAt: number;     // sıralama için (FIFO)
  error?: string;
  attempts: number;
}

class ToptanciDb extends Dexie {
  variants!: Table<Variant, string>;
  customers!: Table<Customer, string>;
  warehouses!: Table<Warehouse, string>;
  queue!: Table<QueuedOperation, string>;
  meta!: Table<{ key: string; value: string }, string>;

  constructor() {
    super('toptanci');
    this.version(1).stores({
      variants: 'id, adetBarcode, koliBarcode, productName',
      customers: 'id, name',
      warehouses: 'id',
      queue: 'id, status, createdAt',
      meta: 'key',
    });
  }
}

export const db = new ToptanciDb();

export async function setMeta(key: string, value: string) {
  await db.meta.put({ key, value });
}
export async function getMeta(key: string): Promise<string | undefined> {
  return (await db.meta.get(key))?.value;
}
