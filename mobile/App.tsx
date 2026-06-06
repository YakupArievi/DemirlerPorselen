import { useState } from 'react';
import { SafeAreaView, Text, TextInput, TouchableOpacity, View, FlatList, StyleSheet } from 'react-native';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { api, login } from './src/api';

const qc = new QueryClient();

/**
 * Faz 7 — Müşteri mobil uygulaması İSKELET'i (Expo + React Native).
 * Kapsam: giriş, borç görüntüleme, cari ekstre, alınan ürünler.
 * Mevcut backend API'lerini kullanır. Çalıştırma: cd mobile && npm install && npm start
 */
export default function App() {
  return (
    <QueryClientProvider client={qc}>
      <Root />
    </QueryClientProvider>
  );
}

function Root() {
  const [loggedIn, setLoggedIn] = useState(false);
  return loggedIn ? <Home /> : <Login onLogin={() => setLoggedIn(true)} />;
}

function Login({ onLogin }: { onLogin: () => void }) {
  const [u, setU] = useState('admin');
  const [p, setP] = useState('Admin123!');
  const [err, setErr] = useState('');
  const submit = async () => {
    try { await login(u, p); onLogin(); } catch { setErr('Giriş başarısız'); }
  };
  return (
    <SafeAreaView style={s.container}>
      <Text style={s.title}>Toptancı</Text>
      {err ? <Text style={s.err}>{err}</Text> : null}
      <TextInput style={s.input} value={u} onChangeText={setU} placeholder="Kullanıcı adı" autoCapitalize="none" />
      <TextInput style={s.input} value={p} onChangeText={setP} placeholder="Parola" secureTextEntry />
      <TouchableOpacity style={s.btn} onPress={submit}><Text style={s.btnText}>Giriş</Text></TouchableOpacity>
    </SafeAreaView>
  );
}

function Home() {
  // NOT: gerçek uygulamada müşteri kendi kaydına bağlanır; iskelette ilk müşteri gösterilir.
  const customers = useQuery({ queryKey: ['m-customers'], queryFn: async () => (await api.get('/customers?pageSize=20')).data });
  return (
    <SafeAreaView style={s.container}>
      <Text style={s.title}>Cari Durum</Text>
      <FlatList
        data={customers.data?.items ?? []}
        keyExtractor={(c: any) => c.id}
        renderItem={({ item }: any) => (
          <View style={s.row}>
            <Text style={s.name}>{item.name}</Text>
            <Text style={[s.balance, { color: item.balance > 0 ? '#d97706' : '#059669' }]}>
              {item.balance.toFixed(2)} TL
            </Text>
          </View>
        )}
      />
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, padding: 20, backgroundColor: '#f1f5f9' },
  title: { fontSize: 24, fontWeight: 'bold', marginBottom: 16, color: '#0f172a' },
  input: { backgroundColor: '#fff', borderRadius: 8, padding: 12, marginBottom: 12, borderWidth: 1, borderColor: '#cbd5e1' },
  btn: { backgroundColor: '#0284c7', borderRadius: 8, padding: 14, alignItems: 'center' },
  btnText: { color: '#fff', fontWeight: '600' },
  err: { color: '#dc2626', marginBottom: 8 },
  row: { flexDirection: 'row', justifyContent: 'space-between', backgroundColor: '#fff', padding: 14, borderRadius: 8, marginBottom: 8 },
  name: { fontWeight: '500', color: '#0f172a' },
  balance: { fontWeight: '700' },
});
