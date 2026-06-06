import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useOffline } from './store';
import { cacheMasterData, syncQueue } from './sync';

/**
 * Online/offline takibi, periyodik senkron ve master veri cache'i.
 * Layout içinde (giriş sonrası) bir kez kurulur.
 */
export function useSyncEngine() {
  const qc = useQueryClient();
  const { setOnline, refreshPending } = useOffline();

  useEffect(() => {
    let cancelled = false;

    const flush = async () => {
      if (!navigator.onLine) return;
      const res = await syncQueue();
      await refreshPending();
      if (res.synced > 0) qc.invalidateQueries(); // sunucu durumunu tazele
    };

    const goOnline = async () => { setOnline(true); await flush(); };
    const goOffline = () => setOnline(false);

    window.addEventListener('online', goOnline);
    window.addEventListener('offline', goOffline);

    // İlk açılış: cache + flush
    (async () => {
      await refreshPending();
      if (navigator.onLine) {
        try { await cacheMasterData(); } catch { /* sessiz */ }
        if (!cancelled) await flush();
      }
    })();

    const interval = setInterval(flush, 20_000);

    return () => {
      cancelled = true;
      clearInterval(interval);
      window.removeEventListener('online', goOnline);
      window.removeEventListener('offline', goOffline);
    };
  }, [qc, setOnline, refreshPending]);
}
