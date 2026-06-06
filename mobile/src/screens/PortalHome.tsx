import { useState } from 'react';
import { ActivityIndicator, FlatList, Modal, Pressable, SafeAreaView, ScrollView, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { API_BASE, api, apiError, dateStr, getAccessToken, logout, tl, type Session } from '../api';
import { Btn, Card, Center, Field, Row, TabBar, colors, s } from '../ui';

const TABS = [
  { key: 'balance', label: 'Borç' },
  { key: 'statement', label: 'Ekstre' },
  { key: 'sales', label: 'Aldıklarım' },
  { key: 'profile', label: 'Profil' },
];

export function PortalHome({ session, onLogout }: { session: Session; onLogout: () => void }) {
  const [tab, setTab] = useState('balance');
  const doLogout = async () => { await logout(); onLogout(); };

  return (
    <SafeAreaView style={s.fill}>
      <View style={s.header}>
        <Text style={s.h1}>{session.user?.name ?? 'Hesabım'}</Text>
        <Pressable onPress={doLogout}><Text style={{ color: colors.sub }}>Çıkış</Text></Pressable>
      </View>
      <TabBar tabs={TABS} active={tab} onChange={setTab} />
      <View style={{ flex: 1 }}>
        {tab === 'balance' && <BalanceTab />}
        {tab === 'statement' && <StatementTab />}
        {tab === 'sales' && <SalesTab />}
        {tab === 'profile' && <ProfileTab session={session} />}
      </View>
    </SafeAreaView>
  );
}

function BalanceTab() {
  const q = useQuery({ queryKey: ['p-balance'], queryFn: async () => (await api.get('/portal/me/balance')).data as number });
  if (q.isLoading) return <Center><ActivityIndicator color={colors.accent} /></Center>;
  const bal = q.data ?? 0;
  return (
    <View style={{ padding: 16 }}>
      <Card>
        <Text style={{ color: colors.sub }}>Güncel Bakiye</Text>
        <Text style={{ fontSize: 36, fontWeight: 'bold', color: bal > 0 ? colors.warn : colors.ok, marginVertical: 6 }}>{tl(bal)}</Text>
        <Text style={{ color: colors.muted, fontSize: 12 }}>{bal > 0 ? 'Borcunuz bulunuyor' : 'Borcunuz yok'}</Text>
      </Card>
    </View>
  );
}

function StatementTab() {
  const q = useQuery({ queryKey: ['p-statement'], queryFn: async () => (await api.get('/portal/me/statement')).data });
  const [sharing, setSharing] = useState(false);
  const [msg, setMsg] = useState('');

  const sharePdf = async () => {
    setSharing(true); setMsg('');
    try {
      const token = await getAccessToken();
      const uri = FileSystem.cacheDirectory + 'ekstre.pdf';
      const res = await FileSystem.downloadAsync(`${API_BASE}/portal/me/statement.pdf`, uri, { headers: { Authorization: `Bearer ${token}` } });
      if (await Sharing.isAvailableAsync()) await Sharing.shareAsync(res.uri);
      else setMsg('Paylaşım bu cihazda desteklenmiyor.');
    } catch (e) { setMsg(apiError(e)); }
    finally { setSharing(false); }
  };

  if (q.isLoading) return <Center><ActivityIndicator color={colors.accent} /></Center>;
  const st = q.data;
  return (
    <FlatList
      contentContainerStyle={{ padding: 16 }}
      ListHeaderComponent={
        <Card>
          <Row left="Açılış Bakiyesi" right={tl(st?.openingBalance ?? 0)} />
          <Row left="Kapanış Bakiyesi" right={tl(st?.closingBalance ?? 0)} />
          <Btn title={sharing ? 'Hazırlanıyor...' : 'Ekstreyi PDF Paylaş'} onPress={sharePdf} busy={sharing} kind="ghost" />
          {msg ? <Text style={{ color: '#f87171', marginTop: 8 }}>{msg}</Text> : null}
        </Card>
      }
      data={st?.lines ?? []}
      keyExtractor={(_: any, i: number) => String(i)}
      renderItem={({ item }: any) => (
        <Row left={item.type} sub={dateStr(item.date)} right={item.debit ? `+${tl(item.debit)}` : `-${tl(item.credit)}`} />
      )}
      ListEmptyComponent={<Text style={{ color: colors.muted, textAlign: 'center', marginTop: 30 }}>Hareket yok</Text>}
    />
  );
}

function SalesTab() {
  const q = useQuery({ queryKey: ['p-sales'], queryFn: async () => (await api.get('/portal/me/sales')).data });
  const [detailId, setDetailId] = useState<string | null>(null);
  if (q.isLoading) return <Center><ActivityIndicator color={colors.accent} /></Center>;
  return (
    <>
      <FlatList
        contentContainerStyle={{ padding: 16 }}
        data={q.data?.items ?? []}
        keyExtractor={(it: any) => it.id}
        renderItem={({ item }: any) => (
          <Pressable onPress={() => setDetailId(item.id)}>
            <Row left={`Fiş #${item.saleNumber}`} sub={`${dateStr(item.saleDate)} · ${item.status === 'Cancelled' ? 'İptal' : 'Aktif'}`} right={tl(item.grandTotal)} />
          </Pressable>
        )}
        ListEmptyComponent={<Text style={{ color: colors.muted, textAlign: 'center', marginTop: 30 }}>Alınan ürün yok</Text>}
      />
      <SaleDetailModal id={detailId} onClose={() => setDetailId(null)} />
    </>
  );
}

function SaleDetailModal({ id, onClose }: { id: string | null; onClose: () => void }) {
  const q = useQuery({ queryKey: ['p-sale', id], enabled: !!id, queryFn: async () => (await api.get(`/portal/me/sales/${id}`)).data });
  return (
    <Modal visible={!!id} transparent animationType="slide" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: '#000a', justifyContent: 'flex-end' }}>
        <View style={{ backgroundColor: colors.bg, borderTopLeftRadius: 16, borderTopRightRadius: 16, padding: 16, maxHeight: '80%' }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 12 }}>
            <Text style={s.h1}>Fiş #{q.data?.saleNumber ?? ''}</Text>
            <Pressable onPress={onClose}><Text style={{ color: colors.sub, fontSize: 18 }}>✕</Text></Pressable>
          </View>
          {q.isLoading ? <ActivityIndicator color={colors.accent} /> : (
            <ScrollView>
              {(q.data?.lines ?? []).map((l: any) => (
                <Row key={l.id} left={`${l.productName} ${l.color ?? ''} ${l.size ?? ''}`.trim()}
                  sub={`${l.quantity} ${l.unitType} × ${tl(l.unitPrice)}`} right={tl(l.lineTotal)} />
              ))}
              <Card style={{ marginTop: 8 }}>
                <Row left="Ara Toplam" right={tl(q.data?.subTotal ?? 0)} />
                <Row left="İskonto" right={tl(q.data?.discountTotal ?? 0)} />
                <Row left="Genel Toplam" right={tl(q.data?.grandTotal ?? 0)} />
                <Row left="Ödenen" right={tl(q.data?.paidAmount ?? 0)} />
              </Card>
            </ScrollView>
          )}
        </View>
      </View>
    </Modal>
  );
}

function ProfileTab({ session }: { session: Session }) {
  const [cur, setCur] = useState('');
  const [nw, setNw] = useState('');
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  const change = async () => {
    setBusy(true); setMsg(null);
    try {
      await api.post('/portal/me/password', { currentPassword: cur, newPassword: nw });
      setMsg({ ok: true, text: 'Parola değiştirildi.' }); setCur(''); setNw('');
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
    finally { setBusy(false); }
  };

  return (
    <ScrollView contentContainerStyle={{ padding: 16 }}>
      <Card>
        <Row left="Ad" right={session.user?.name} />
        <Row left="Telefon" right={session.user?.phone ?? '-'} />
      </Card>
      <Card>
        <Text style={{ color: colors.text, fontWeight: '600', marginBottom: 10 }}>Parola Değiştir</Text>
        {msg ? <Text style={{ color: msg.ok ? colors.ok : '#f87171', marginBottom: 8 }}>{msg.text}</Text> : null}
        <Field label="Mevcut parola" value={cur} onChangeText={setCur} secureTextEntry />
        <Field label="Yeni parola" value={nw} onChangeText={setNw} secureTextEntry />
        <Btn title="Güncelle" onPress={change} busy={busy} disabled={!cur || !nw} />
      </Card>
    </ScrollView>
  );
}
