import { useEffect, useState } from 'react';
import {
  ActivityIndicator, FlatList, SafeAreaView, StyleSheet, Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { getBalance, getSales, getStatement, hasSession, portalLogin, portalLogout } from './src/api';

const qc = new QueryClient({ defaultOptions: { queries: { retry: 1 } } });
const tl = (v: number) => `${(v ?? 0).toFixed(2)} TL`;
const d = (s: string) => new Date(s).toLocaleDateString('tr-TR');

/**
 * Faz 7 — Müşteri Mobil Portalı (Expo + React Native).
 * Müşteri telefon + parola ile girer; SADECE kendi borcunu/ekstresini/aldığı ürünleri görür.
 * Backend: /api/portal/auth/* ve /api/portal/me/* (ayrı JWT şeması ile izole).
 * Çalıştırma: cd mobile && npm install && npm start
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
  const [authed, setAuthed] = useState<boolean | null>(null);
  useEffect(() => { hasSession().then(setAuthed); }, []);
  if (authed === null) return <Center><ActivityIndicator size="large" color="#0284c7" /></Center>;
  return authed ? <Home onLogout={() => setAuthed(false)} /> : <Login onLogin={() => setAuthed(true)} />;
}

function Login({ onLogin }: { onLogin: () => void }) {
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    setErr(''); setBusy(true);
    try { await portalLogin(phone.trim(), password); onLogin(); }
    catch { setErr('Telefon veya parola hatalı.'); }
    finally { setBusy(false); }
  };

  return (
    <SafeAreaView style={s.container}>
      <View style={{ flex: 1, justifyContent: 'center' }}>
        <Text style={s.brand}>Toptancı</Text>
        <Text style={s.sub}>Müşteri Portalı</Text>
        {err ? <Text style={s.err}>{err}</Text> : null}
        <TextInput style={s.input} value={phone} onChangeText={setPhone} placeholder="Telefon" placeholderTextColor="#64748b" keyboardType="phone-pad" autoCapitalize="none" />
        <TextInput style={s.input} value={password} onChangeText={setPassword} placeholder="Parola" placeholderTextColor="#64748b" secureTextEntry />
        <TouchableOpacity style={[s.btn, busy && { opacity: 0.6 }]} onPress={submit} disabled={busy}>
          <Text style={s.btnText}>{busy ? 'Giriş yapılıyor...' : 'Giriş'}</Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}

type Tab = 'balance' | 'statement' | 'sales';

function Home({ onLogout }: { onLogout: () => void }) {
  const [tab, setTab] = useState<Tab>('balance');
  const logout = async () => { await portalLogout(); onLogout(); };

  return (
    <SafeAreaView style={s.container}>
      <View style={s.header}>
        <Text style={s.headerTitle}>Hesabım</Text>
        <TouchableOpacity onPress={logout}><Text style={s.logout}>Çıkış</Text></TouchableOpacity>
      </View>

      <View style={s.tabs}>
        <TabBtn label="Borç" active={tab === 'balance'} onPress={() => setTab('balance')} />
        <TabBtn label="Ekstre" active={tab === 'statement'} onPress={() => setTab('statement')} />
        <TabBtn label="Aldıklarım" active={tab === 'sales'} onPress={() => setTab('sales')} />
      </View>

      <View style={{ flex: 1 }}>
        {tab === 'balance' && <BalanceTab />}
        {tab === 'statement' && <StatementTab />}
        {tab === 'sales' && <SalesTab />}
      </View>
    </SafeAreaView>
  );
}

function TabBtn({ label, active, onPress }: { label: string; active: boolean; onPress: () => void }) {
  return (
    <TouchableOpacity style={[s.tab, active && s.tabActive]} onPress={onPress}>
      <Text style={[s.tabText, active && s.tabTextActive]}>{label}</Text>
    </TouchableOpacity>
  );
}

function BalanceTab() {
  const q = useQuery({ queryKey: ['balance'], queryFn: getBalance });
  if (q.isLoading) return <Center><ActivityIndicator color="#0284c7" /></Center>;
  const bal = q.data ?? 0;
  return (
    <View style={{ padding: 20 }}>
      <View style={s.card}>
        <Text style={s.cardLabel}>Güncel Bakiye</Text>
        <Text style={[s.cardValue, { color: bal > 0 ? '#d97706' : '#059669' }]}>{tl(bal)}</Text>
        <Text style={s.cardHint}>{bal > 0 ? 'Borcunuz bulunuyor' : 'Borcunuz yok'}</Text>
      </View>
    </View>
  );
}

function StatementTab() {
  const q = useQuery({ queryKey: ['statement'], queryFn: getStatement });
  if (q.isLoading) return <Center><ActivityIndicator color="#0284c7" /></Center>;
  const st = q.data;
  return (
    <FlatList
      contentContainerStyle={{ padding: 16 }}
      ListHeaderComponent={
        <View style={s.card}>
          <Text style={s.cardLabel}>Dönem Bakiyesi</Text>
          <Text style={s.row}>Açılış: {tl(st?.openingBalance ?? 0)}</Text>
          <Text style={s.row}>Kapanış: {tl(st?.closingBalance ?? 0)}</Text>
        </View>
      }
      data={st?.lines ?? []}
      keyExtractor={(_: any, i: number) => String(i)}
      renderItem={({ item }: any) => (
        <View style={s.listRow}>
          <View>
            <Text style={s.listMain}>{item.type}</Text>
            <Text style={s.listSub}>{d(item.date)}</Text>
          </View>
          <Text style={{ color: item.debit ? '#dc2626' : '#059669' }}>
            {item.debit ? `+${tl(item.debit)}` : `-${tl(item.credit)}`}
          </Text>
        </View>
      )}
      ListEmptyComponent={<Text style={s.empty}>Hareket yok</Text>}
    />
  );
}

function SalesTab() {
  const q = useQuery({ queryKey: ['sales'], queryFn: getSales });
  if (q.isLoading) return <Center><ActivityIndicator color="#0284c7" /></Center>;
  return (
    <FlatList
      contentContainerStyle={{ padding: 16 }}
      data={q.data?.items ?? []}
      keyExtractor={(it: any) => it.id}
      renderItem={({ item }: any) => (
        <View style={s.listRow}>
          <View>
            <Text style={s.listMain}>Fiş #{item.saleNumber}</Text>
            <Text style={s.listSub}>{d(item.saleDate)} · {item.status === 'Cancelled' ? 'İptal' : 'Aktif'}</Text>
          </View>
          <Text style={s.listMain}>{tl(item.grandTotal)}</Text>
        </View>
      )}
      ListEmptyComponent={<Text style={s.empty}>Alınan ürün/fiş yok</Text>}
    />
  );
}

function Center({ children }: { children: React.ReactNode }) {
  return <View style={[s.container, { justifyContent: 'center', alignItems: 'center' }]}>{children}</View>;
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0f172a' },
  brand: { fontSize: 34, fontWeight: 'bold', color: '#fff', textAlign: 'center' },
  sub: { fontSize: 14, color: '#94a3b8', textAlign: 'center', marginBottom: 24 },
  input: { backgroundColor: '#1e293b', color: '#fff', borderRadius: 10, padding: 14, marginHorizontal: 24, marginBottom: 12 },
  btn: { backgroundColor: '#0284c7', borderRadius: 10, padding: 16, alignItems: 'center', marginHorizontal: 24, marginTop: 4 },
  btnText: { color: '#fff', fontWeight: '700', fontSize: 16 },
  err: { color: '#f87171', textAlign: 'center', marginBottom: 8 },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 16, paddingTop: 24 },
  headerTitle: { color: '#fff', fontSize: 22, fontWeight: 'bold' },
  logout: { color: '#94a3b8' },
  tabs: { flexDirection: 'row', backgroundColor: '#1e293b', marginHorizontal: 16, borderRadius: 10, padding: 4 },
  tab: { flex: 1, padding: 10, borderRadius: 8, alignItems: 'center' },
  tabActive: { backgroundColor: '#0284c7' },
  tabText: { color: '#94a3b8', fontWeight: '600' },
  tabTextActive: { color: '#fff' },
  card: { backgroundColor: '#1e293b', borderRadius: 12, padding: 20, marginBottom: 12 },
  cardLabel: { color: '#94a3b8', fontSize: 13 },
  cardValue: { fontSize: 36, fontWeight: 'bold', marginVertical: 6 },
  cardHint: { color: '#64748b', fontSize: 12 },
  row: { color: '#e2e8f0', marginTop: 4 },
  listRow: { flexDirection: 'row', justifyContent: 'space-between', backgroundColor: '#1e293b', padding: 14, borderRadius: 10, marginBottom: 8 },
  listMain: { color: '#fff', fontWeight: '600' },
  listSub: { color: '#94a3b8', fontSize: 12, marginTop: 2 },
  empty: { color: '#64748b', textAlign: 'center', marginTop: 40 },
});
