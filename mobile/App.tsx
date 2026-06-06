import { useEffect, useState } from 'react';
import { ActivityIndicator } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { getSession, type Session } from './src/api';
import { Center, colors } from './src/ui';
import { Login } from './src/screens/Login';
import { PortalHome } from './src/screens/PortalHome';
import { StaffHome } from './src/screens/StaffHome';

const qc = new QueryClient({ defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } } });

/**
 * Toptancı mobil — tek uygulama, rol bazlı:
 *  - Müşteri girişi  -> PortalHome (borç/ekstre/aldıklarım/profil)
 *  - Personel girişi -> StaffHome (panel/satış/cari/stok)
 */
export default function App() {
  return (
    <QueryClientProvider client={qc}>
      <StatusBar style="light" />
      <Root />
    </QueryClientProvider>
  );
}

function Root() {
  const [loading, setLoading] = useState(true);
  const [session, setSession] = useState<Session | null>(null);

  useEffect(() => { getSession().then((sess) => { setSession(sess); setLoading(false); }); }, []);

  if (loading) return <Center><ActivityIndicator size="large" color={colors.accent} /></Center>;
  if (!session) return <Login onLogin={setSession} />;
  return session.mode === 'staff'
    ? <StaffHome session={session} onLogout={() => setSession(null)} />
    : <PortalHome session={session} onLogout={() => setSession(null)} />;
}
