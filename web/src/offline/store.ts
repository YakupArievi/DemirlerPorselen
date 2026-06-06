import { create } from 'zustand';
import { pendingCount } from './sync';

interface OfflineState {
  online: boolean;
  pending: number;
  setOnline: (v: boolean) => void;
  refreshPending: () => Promise<void>;
}

export const useOffline = create<OfflineState>((set) => ({
  online: typeof navigator !== 'undefined' ? navigator.onLine : true,
  pending: 0,
  setOnline: (v) => set({ online: v }),
  refreshPending: async () => set({ pending: await pendingCount() }),
}));
