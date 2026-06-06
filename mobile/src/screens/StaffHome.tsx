import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Modal, Pressable, SafeAreaView, ScrollView, Text, View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiError, dateStr, logout, tl, type Session } from '../api';
import { Btn, Card, Center, Field, Row, TabBar, colors, s } from '../ui';

const TABS = [
  { key: 'dash', label: 'Panel' },
  { key: 'sale', label: 'Satış' },
  { key: 'cari', label: 'Cari' },
  { key: 'stok', label: 'Stok' },
];

export function StaffHome({ session, onLogout }: { session: Session; onLogout: () => void }) {
  const [tab, setTab] = useState('dash');
  const doLogout = async () => { await logout(); onLogout(); };
  return (
    <SafeAreaView style={s.fill}>
      <View style={s.header}>
        <Text style={s.h1}>{session.user?.fullName ?? 'Yönetim'}</Text>
        <Pressable onPress={doLogout}><Text style={{ color: colors.sub }}>Çıkış</Text></Pressable>
      </View>
      <TabBar tabs={TABS} active={tab} onChange={setTab} />
      <View style={{ flex: 1 }}>
        {tab === 'dash' && <DashTab />}
        {tab === 'sale' && <SaleTab />}
        {tab === 'cari' && <CariTab />}
        {tab === 'stok' && <StokTab />}
      </View>
    </SafeAreaView>
  );
}

function DashTab() {
  const q = useQuery({ queryKey: ['s-dash'], queryFn: async () => (await api.get('/dashboard/summary')).data });
  if (q.isLoading || !q.data) return <Center><ActivityIndicator color={colors.accent} /></Center>;
  const d = q.data;
  const Stat = ({ t, v, c }: { t: string; v: string; c?: string }) => (
    <Card style={{ flex: 1, minWidth: '44%' }}>
      <Text style={{ color: colors.sub, fontSize: 12 }}>{t}</Text>
      <Text style={{ color: c ?? colors.text, fontSize: 20, fontWeight: 'bold', marginTop: 4 }}>{v}</Text>
    </Card>
  );
  return (
    <ScrollView contentContainerStyle={{ padding: 12 }}>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        <Stat t="Bugün Ciro" v={tl(d.todayRevenue)} c={colors.accent2} />
        <Stat t="Bu Ay Ciro" v={tl(d.monthRevenue)} c={colors.accent2} />
        <Stat t="Bugün Tahsilat" v={tl(d.todayCollections)} c={colors.ok} />
        <Stat t="Toplam Alacak" v={tl(d.totalReceivables)} c={colors.warn} />
        <Stat t="Kritik Stok" v={`${d.criticalStockCount}`} c={colors.danger} />
        <Stat t="Bu Ay Kırık" v={`${d.brokenQuantityThisMonth}`} c={colors.danger} />
      </View>
      <Text style={{ color: colors.text, fontWeight: '600', marginTop: 12, marginBottom: 6 }}>Son Satışlar</Text>
      {(d.recentSales ?? []).map((x: any) => (
        <Row key={x.id} left={`#${x.saleNumber} ${x.customerName}`} sub={dateStr(x.saleDate)} right={tl(x.grandTotal)} />
      ))}
    </ScrollView>
  );
}

/** Basit modal liste seçici */
function Selector<T>({ visible, title, items, label, onPick, onClose }: {
  visible: boolean; title: string; items: T[]; label: (x: T) => string; onPick: (x: T) => void; onClose: () => void;
}) {
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: '#000a', justifyContent: 'flex-end' }}>
        <View style={{ backgroundColor: colors.bg, borderTopLeftRadius: 16, borderTopRightRadius: 16, padding: 16, maxHeight: '70%' }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 }}>
            <Text style={s.h1}>{title}</Text>
            <Pressable onPress={onClose}><Text style={{ color: colors.sub, fontSize: 18 }}>✕</Text></Pressable>
          </View>
          <FlatList data={items} keyExtractor={(_, i) => String(i)}
            renderItem={({ item }) => (
              <Pressable onPress={() => { onPick(item); onClose(); }}><Row left={label(item)} /></Pressable>
            )} />
        </View>
      </View>
    </Modal>
  );
}

interface CartItem { key: string; variantId: string; name: string; unitType: string; quantity: number; unitPrice: number; }

function SaleTab() {
  const qc = useQueryClient();
  const customers = useQuery({ queryKey: ['s-custs'], queryFn: async () => (await api.get('/customers?pageSize=500')).data.items });
  const warehouses = useQuery({ queryKey: ['s-whs'], queryFn: async () => (await api.get('/warehouses')).data });
  const [customer, setCustomer] = useState<any>(null);
  const [wh, setWh] = useState<any>(null);
  const [pickCust, setPickCust] = useState(false);
  const [barcode, setBarcode] = useState('');
  const [cart, setCart] = useState<CartItem[]>([]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => { if (warehouses.data && !wh) setWh(warehouses.data.find((w: any) => w.isDefault) ?? warehouses.data[0]); }, [warehouses.data, wh]);

  const add = async () => {
    if (!barcode.trim()) return;
    try {
      const r = (await api.get(`/variants/resolve?barcode=${encodeURIComponent(barcode.trim())}`)).data;
      const price = r.unitType === 'Koli' ? r.salePrice * r.adetEquivalent : r.salePrice;
      setCart((c) => [...c, { key: Math.random().toString(36), variantId: r.variantId, name: `${r.productName} ${r.color ?? ''} (${r.unitType})`.trim(), unitType: r.unitType, quantity: 1, unitPrice: price }]);
      setBarcode('');
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
  };
  const total = cart.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0);

  const submit = async () => {
    setMsg(null);
    if (!customer) return setMsg({ ok: false, text: 'Müşteri seçin.' });
    if (!cart.length) return setMsg({ ok: false, text: 'Sepet boş.' });
    setBusy(true);
    try {
      const res = await api.post('/sales', {
        customerId: customer.id, warehouseId: wh.id, idempotencyKey: Math.random().toString(36) + Date.now(),
        documentDiscount: 0, lines: cart.map((i) => ({ variantId: i.variantId, unitType: i.unitType, quantity: i.quantity, unitPrice: i.unitPrice, lineDiscount: 0 })),
      });
      setMsg({ ok: true, text: `Satış #${res.data.saleNumber} kaydedildi (${tl(res.data.grandTotal)})` });
      setCart([]); qc.invalidateQueries({ queryKey: ['s-dash'] });
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
    finally { setBusy(false); }
  };

  return (
    <ScrollView contentContainerStyle={{ padding: 16 }}>
      {msg ? <Text style={{ color: msg.ok ? colors.ok : '#f87171', marginBottom: 10 }}>{msg.text}</Text> : null}
      <Pressable onPress={() => setPickCust(true)}><Field label="Müşteri" editable={false} pointerEvents="none" value={customer?.name ?? ''} placeholder="Müşteri seç..." /></Pressable>
      <Text style={{ color: colors.sub, fontSize: 12, marginBottom: 8 }}>Depo: {wh?.name ?? '...'}</Text>
      <Field label="Barkod" value={barcode} onChangeText={setBarcode} onSubmitEditing={add} returnKeyType="done" placeholder="Barkod oku/yaz + enter" />
      {cart.map((i) => (
        <View key={i.key} style={[s.row]}>
          <View style={{ flex: 1 }}>
            <Text style={s.rowMain}>{i.name}</Text>
            <Text style={s.rowSub}>{tl(i.unitPrice)} × {i.quantity}</Text>
          </View>
          <Pressable onPress={() => setCart((c) => c.map((x) => x.key === i.key ? { ...x, quantity: Math.max(1, x.quantity - 1) } : x))}><Text style={{ color: colors.accent2, fontSize: 22, paddingHorizontal: 8 }}>−</Text></Pressable>
          <Text style={{ color: colors.text }}>{i.quantity}</Text>
          <Pressable onPress={() => setCart((c) => c.map((x) => x.key === i.key ? { ...x, quantity: x.quantity + 1 } : x))}><Text style={{ color: colors.accent2, fontSize: 22, paddingHorizontal: 8 }}>+</Text></Pressable>
          <Pressable onPress={() => setCart((c) => c.filter((x) => x.key !== i.key))}><Text style={{ color: colors.danger, paddingLeft: 6 }}>✕</Text></Pressable>
        </View>
      ))}
      <Card style={{ marginTop: 8 }}>
        <Row left="Toplam" right={tl(total)} />
        <Btn title="Satışı Tamamla" onPress={submit} busy={busy} />
      </Card>
      <Selector visible={pickCust} title="Müşteri Seç" items={customers.data ?? []} label={(c: any) => `${c.name} (${tl(c.balance)})`} onPick={setCustomer} onClose={() => setPickCust(false)} />
    </ScrollView>
  );
}

function CariTab() {
  const [search, setSearch] = useState('');
  const [sel, setSel] = useState<any>(null);
  const q = useQuery({ queryKey: ['s-cari', search], queryFn: async () => (await api.get(`/customers?pageSize=100&search=${encodeURIComponent(search)}`)).data.items });
  return (
    <View style={{ flex: 1, padding: 16 }}>
      <Field value={search} onChangeText={setSearch} placeholder="Müşteri ara..." />
      <FlatList data={q.data ?? []} keyExtractor={(c: any) => c.id}
        renderItem={({ item }: any) => (
          <Pressable onPress={() => setSel(item)}>
            <Row left={item.name} sub={item.phone ?? '-'} right={tl(item.balance)} />
          </Pressable>
        )}
        ListEmptyComponent={<Text style={{ color: colors.muted, textAlign: 'center', marginTop: 20 }}>Müşteri yok</Text>} />
      <CariDetailModal customer={sel} onClose={() => setSel(null)} />
    </View>
  );
}

function CariDetailModal({ customer, onClose }: { customer: any; onClose: () => void }) {
  const qc = useQueryClient();
  const [amount, setAmount] = useState('');
  const [type, setType] = useState('Nakit');
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState('');
  const bal = useQuery({ queryKey: ['s-cari-bal', customer?.id], enabled: !!customer, queryFn: async () => (await api.get(`/customers/${customer.id}/balance`)).data as number });

  const collect = async () => {
    setBusy(true); setMsg('');
    try {
      await api.post('/payments', { customerId: customer.id, type, amount: Number(amount), idempotencyKey: Math.random().toString(36) + Date.now() });
      setMsg('Tahsilat kaydedildi.'); setAmount('');
      bal.refetch(); qc.invalidateQueries({ queryKey: ['s-cari'] }); qc.invalidateQueries({ queryKey: ['s-dash'] });
    } catch (e) { setMsg(apiError(e)); }
    finally { setBusy(false); }
  };

  return (
    <Modal visible={!!customer} transparent animationType="slide" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: '#000a', justifyContent: 'flex-end' }}>
        <View style={{ backgroundColor: colors.bg, borderTopLeftRadius: 16, borderTopRightRadius: 16, padding: 16 }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 12 }}>
            <Text style={s.h1}>{customer?.name}</Text>
            <Pressable onPress={onClose}><Text style={{ color: colors.sub, fontSize: 18 }}>✕</Text></Pressable>
          </View>
          <Card><Row left="Güncel Bakiye" right={tl(bal.data ?? customer?.balance ?? 0)} /></Card>
          <Card>
            <Text style={{ color: colors.text, fontWeight: '600', marginBottom: 10 }}>Tahsilat</Text>
            {msg ? <Text style={{ color: colors.ok, marginBottom: 8 }}>{msg}</Text> : null}
            <View style={{ flexDirection: 'row', gap: 6, marginBottom: 10 }}>
              {['Nakit', 'Kart', 'Cek'].map((t) => (
                <Pressable key={t} onPress={() => setType(t)} style={{ flex: 1, padding: 10, borderRadius: 8, alignItems: 'center', backgroundColor: type === t ? colors.accent : colors.card }}>
                  <Text style={{ color: type === t ? '#fff' : colors.sub }}>{t}</Text>
                </Pressable>
              ))}
            </View>
            <Field value={amount} onChangeText={setAmount} keyboardType="numeric" placeholder="Tutar" />
            <Btn title="Tahsilat Ekle" onPress={collect} busy={busy} disabled={!Number(amount)} />
          </Card>
        </View>
      </View>
    </Modal>
  );
}

interface EntryLine { key: string; variantId: string; name: string; unitType: string; quantity: number; unitPurchasePrice: number; }

function StokTab() {
  const warehouses = useQuery({ queryKey: ['s-whs'], queryFn: async () => (await api.get('/warehouses')).data });
  const [wh, setWh] = useState<any>(null);
  const [barcode, setBarcode] = useState('');
  const [lines, setLines] = useState<EntryLine[]>([]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  useEffect(() => { if (warehouses.data && !wh) setWh(warehouses.data.find((w: any) => w.isDefault) ?? warehouses.data[0]); }, [warehouses.data, wh]);
  const stock = useQuery({ queryKey: ['s-stock', wh?.id], enabled: !!wh, queryFn: async () => (await api.get(`/stock/warehouse/${wh.id}?pageSize=200`)).data.items });

  const add = async () => {
    if (!barcode.trim()) return;
    try {
      const r = (await api.get(`/variants/resolve?barcode=${encodeURIComponent(barcode.trim())}`)).data;
      setLines((l) => [...l, { key: Math.random().toString(36), variantId: r.variantId, name: `${r.productName} (${r.unitType})`, unitType: r.unitType, quantity: 1, unitPurchasePrice: 0 }]);
      setBarcode('');
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
  };
  const submit = async () => {
    setMsg(null);
    if (!wh || !lines.length) return setMsg({ ok: false, text: 'Satır ekleyin.' });
    try {
      await api.post('/stock/entry', { warehouseId: wh.id, idempotencyKey: Math.random().toString(36) + Date.now(), lines: lines.map((l) => ({ variantId: l.variantId, unitType: l.unitType, quantity: l.quantity, unitPurchasePrice: l.unitPurchasePrice })) });
      setMsg({ ok: true, text: 'Giriş kaydedildi.' }); setLines([]); stock.refetch();
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
  };

  return (
    <ScrollView contentContainerStyle={{ padding: 16 }}>
      <Text style={{ color: colors.sub, fontSize: 12, marginBottom: 8 }}>Depo: {wh?.name ?? '...'}</Text>
      {msg ? <Text style={{ color: msg.ok ? colors.ok : '#f87171', marginBottom: 8 }}>{msg.text}</Text> : null}
      <Card>
        <Text style={{ color: colors.text, fontWeight: '600', marginBottom: 8 }}>Ürün Girişi</Text>
        <Field value={barcode} onChangeText={setBarcode} onSubmitEditing={add} returnKeyType="done" placeholder="Barkod + enter" />
        {lines.map((l) => (
          <View key={l.key} style={s.row}>
            <View style={{ flex: 1 }}><Text style={s.rowMain}>{l.name}</Text></View>
            <Field value={String(l.quantity)} onChangeText={(v) => setLines((ls) => ls.map((x) => x.key === l.key ? { ...x, quantity: Number(v) || 0 } : x))} keyboardType="numeric" style={{ width: 56, marginBottom: 0 }} />
            <Field value={String(l.unitPurchasePrice)} onChangeText={(v) => setLines((ls) => ls.map((x) => x.key === l.key ? { ...x, unitPurchasePrice: Number(v) || 0 } : x))} keyboardType="numeric" style={{ width: 72, marginBottom: 0, marginLeft: 6 }} />
          </View>
        ))}
        <Btn title="Girişi Kaydet" onPress={submit} />
      </Card>
      <Text style={{ color: colors.text, fontWeight: '600', marginTop: 8, marginBottom: 6 }}>Depo Stoğu</Text>
      {(stock.data ?? []).map((x: any) => (
        <Row key={x.variantId} left={`${x.productName} ${x.color ?? ''}`.trim()} sub={x.adetBarcode} right={`${x.quantity}`} />
      ))}
    </ScrollView>
  );
}
