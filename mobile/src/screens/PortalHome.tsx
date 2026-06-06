import { useState } from 'react';
import { FlatList, Modal, Pressable, SafeAreaView, ScrollView, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { API_BASE, api, apiError, dateStr, getAccessToken, logout, tl, type Session } from '../api';
import { Btn, BottomTabs, Card, Empty, Field, Loading, Row, colors, refresh, s } from '../ui';

const TABS = [
  { key: 'balance', label: 'Borç', icon: 'wallet' },
  { key: 'statement', label: 'Ekstre', icon: 'document-text' },
  { key: 'sales', label: 'Aldıklarım', icon: 'cart' },
  { key: 'profile', label: 'Profil', icon: 'person' },
];

export function PortalHome({ session, onLogout }: { session: Session; onLogout: () => void }) {
  const [tab, setTab] = useState('balance');
  const doLogout = async () => { await logout(); onLogout(); };
  return (
    <SafeAreaView style={s.fill}>
      <View style={s.header}>
        <View>
          <Text style={s.h1}>{session.user?.name ?? 'Hesabım'}</Text>
          <Text style={{ color: colors.sub, fontSize: 12 }}>Müşteri Portalı</Text>
        </View>
        <Pressable onPress={doLogout} hitSlop={10}><Text style={{ color: colors.sub }}>Çıkış</Text></Pressable>
      </View>
      <View style={{ flex: 1 }}>
        {tab === 'balance' && <BalanceTab />}
        {tab === 'statement' && <StatementTab />}
        {tab === 'sales' && <SalesTab />}
        {tab === 'profile' && <ProfileTab session={session} />}
      </View>
      <BottomTabs tabs={TABS} active={tab} onChange={setTab} />
    </SafeAreaView>
  );
}

function BalanceTab() {
  const q = useQuery({ queryKey: ['p-balance'], queryFn: async () => (await api.get('/portal/me/balance')).data as number });
  if (q.isLoading) return <Loading />;
  const bal = q.data ?? 0;
  return (
    <ScrollView contentContainerStyle={{ padding: 16 }} refreshControl={refresh(q.refetch, q.isRefetching)}>
      <Card style={{ alignItems: 'center', paddingVertical: 28 }}>
        <Text style={{ color: colors.sub }}>Güncel Bakiye</Text>
        <Text style={{ fontSize: 40, fontWeight: 'bold', color: bal > 0 ? colors.warn : colors.ok, marginVertical: 8 }}>{tl(bal)}</Text>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
          <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: bal > 0 ? colors.warn : colors.ok }} />
          <Text style={{ color: colors.muted, fontSize: 13 }}>{bal > 0 ? 'Borcunuz bulunuyor' : 'Borcunuz yok'}</Text>
        </View>
      </Card>
    </ScrollView>
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

  if (q.isLoading) return <Loading />;
  const st = q.data;
  return (
    <FlatList
      contentContainerStyle={{ padding: 16 }}
      refreshControl={refresh(q.refetch, q.isRefetching)}
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
      ListEmptyComponent={<Empty icon="receipt-outline" text="Hareket yok" />}
    />
  );
}

function SalesTab() {
  const q = useQuery({ queryKey: ['p-sales'], queryFn: async () => (await api.get('/portal/me/sales')).data });
  const [detailId, setDetailId] = useState<string | null>(null);
  if (q.isLoading) return <Loading />;
  return (
    <>
      <FlatList
        contentContainerStyle={{ padding: 16 }}
        refreshControl={refresh(q.refetch, q.isRefetching)}
        data={q.data?.items ?? []}
        keyExtractor={(it: any) => it.id}
        renderItem={({ item }: any) => (
          <Pressable onPress={() => setDetailId(item.id)}>
            <Row left={`Fiş #${item.saleNumber}`} sub={`${dateStr(item.saleDate)} · ${item.status === 'Cancelled' ? 'İptal' : 'Aktif'}`} right={tl(item.grandTotal)} />
          </Pressable>
        )}
        ListEmptyComponent={<Empty icon="cart-outline" text="Alınan ürün yok" />}
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
            <Pressable onPress={onClose} hitSlop={10}><Text style={{ color: colors.sub, fontSize: 18 }}>✕</Text></Pressable>
          </View>
          {q.isLoading ? <Loading /> : (
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
