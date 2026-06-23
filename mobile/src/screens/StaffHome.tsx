import { useEffect, useState } from 'react';
import { FlatList, Modal, Pressable, SafeAreaView, ScrollView, Text, View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiError, dateStr, logout, tl, type Session } from '../api';
import { Btn, BottomTabs, Card, Empty, Field, Loading, Row, StatCard, colors, refresh, s } from '../ui';
import { QrScanner } from './QrScanner';

const ALL_TABS = [
  { key: 'dash', label: 'Panel', icon: 'grid' },
  { key: 'sale', label: 'Satış', icon: 'cart' },
  { key: 'cari', label: 'Cari', icon: 'people' },
  { key: 'stok', label: 'Stok', icon: 'cube' },
];

export function StaffHome({ session, onLogout }: { session: Session; onLogout: () => void }) {
  // Depocu: cari/panel (parasal) gizli -> sadece Satış + Stok
  const isDepocu = session.user?.role === 'Depocu';
  const tabs = isDepocu ? ALL_TABS.filter((t) => t.key === 'sale' || t.key === 'stok') : ALL_TABS;
  const [tab, setTab] = useState(tabs[0].key);
  const doLogout = async () => { await logout(); onLogout(); };
  return (
    <SafeAreaView style={s.fill}>
      <View style={s.header}>
        <View>
          <Text style={s.h1}>{session.user?.fullName ?? 'Yönetim'}</Text>
          <Text style={{ color: colors.sub, fontSize: 12 }}>{session.user?.role ?? 'Personel'}</Text>
        </View>
        <Pressable onPress={doLogout} hitSlop={10}><Text style={{ color: colors.sub }}>Çıkış</Text></Pressable>
      </View>
      <View style={{ flex: 1 }}>
        {tab === 'dash' && <DashTab />}
        {tab === 'sale' && <SaleTab />}
        {tab === 'cari' && <CariTab />}
        {tab === 'stok' && <StokTab />}
      </View>
      <BottomTabs tabs={tabs} active={tab} onChange={setTab} />
    </SafeAreaView>
  );
}

function DashTab() {
  const q = useQuery({ queryKey: ['s-dash'], queryFn: async () => (await api.get('/dashboard/summary')).data, refetchInterval: 30000 });
  if (q.isLoading || !q.data) return <Loading />;
  const d = q.data;
  return (
    <ScrollView contentContainerStyle={{ padding: 12 }} refreshControl={refresh(q.refetch, q.isRefetching)}>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        <StatCard title="Bugün Ciro" value={tl(d.todayRevenue)} color={colors.accent2} icon="cash" />
        <StatCard title="Bu Ay Ciro" value={tl(d.monthRevenue)} color={colors.accent2} icon="trending-up" />
        <StatCard title="Bugün Tahsilat" value={tl(d.todayCollections)} color={colors.ok} icon="wallet" />
        <StatCard title="Toplam Alacak" value={tl(d.totalReceivables)} color={colors.warn} icon="alert-circle" />
        <StatCard title="Kritik Stok" value={`${d.criticalStockCount} ürün`} color={colors.danger} icon="warning" />
        <StatCard title="Bu Ay Kırık" value={`${d.brokenQuantityThisMonth} adet`} color={colors.danger} icon="trash" />
      </View>

      <Text style={s.section}>Kritik Stoklar</Text>
      {(d.criticalStocks ?? []).length === 0 ? <Text style={s.dim}>Yok</Text> :
        (d.criticalStocks ?? []).map((c: any) => (
          <Row key={c.variantId} left={c.productName} right={`${c.totalQuantity}/${c.minStock}`} />
        ))}

      <Text style={s.section}>En Çok Satanlar (Bu Ay)</Text>
      {(d.topProducts ?? []).length === 0 ? <Text style={s.dim}>Veri yok</Text> :
        (d.topProducts ?? []).map((p: any) => (
          <Row key={p.variantId} left={p.productName} sub={`${p.soldQuantity} adet`} right={tl(p.revenue)} />
        ))}

      <Text style={s.section}>Son Satışlar</Text>
      {(d.recentSales ?? []).map((x: any) => (
        <Row key={x.id} left={`#${x.saleNumber} ${x.customerName}`} sub={dateStr(x.saleDate)} right={tl(x.grandTotal)} />
      ))}
    </ScrollView>
  );
}

function Selector<T>({ visible, title, items, label, onPick, onClose }: {
  visible: boolean; title: string; items: T[]; label: (x: T) => string; onPick: (x: T) => void; onClose: () => void;
}) {
  const [q, setQ] = useState('');
  const filtered = q ? items.filter((x) => label(x).toLowerCase().includes(q.toLowerCase())) : items;
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: '#000a', justifyContent: 'flex-end' }}>
        <View style={{ backgroundColor: colors.bg, borderTopLeftRadius: 16, borderTopRightRadius: 16, padding: 16, maxHeight: '75%' }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 10 }}>
            <Text style={s.h1}>{title}</Text>
            <Pressable onPress={onClose} hitSlop={10}><Text style={{ color: colors.sub, fontSize: 18 }}>✕</Text></Pressable>
          </View>
          <Field value={q} onChangeText={setQ} placeholder="Ara..." />
          <FlatList data={filtered} keyExtractor={(_, i) => String(i)}
            renderItem={({ item }) => (
              <Pressable onPress={() => { onPick(item); onClose(); setQ(''); }}><Row left={label(item)} /></Pressable>
            )} />
        </View>
      </View>
    </Modal>
  );
}

interface CartItem { key: string; variantId: string; name: string; unitType: string; quantity: number; unitPrice: number; }

function SaleTab() {
  const qc = useQueryClient();
  // Lookup: id+ad (Depocu dahil herkes erişebilir; parasal bilgi yok)
  const customers = useQuery({ queryKey: ['s-lookup'], queryFn: async () => (await api.get('/customers/lookup')).data });
  const warehouses = useQuery({ queryKey: ['s-whs'], queryFn: async () => (await api.get('/warehouses')).data });
  const [customer, setCustomer] = useState<any>(null);
  const [wh, setWh] = useState<any>(null);
  const [pickCust, setPickCust] = useState(false);
  const [barcode, setBarcode] = useState('');
  const [scan, setScan] = useState(false);
  const [cart, setCart] = useState<CartItem[]>([]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => { if (warehouses.data && !wh) setWh(warehouses.data.find((w: any) => w.isDefault) ?? warehouses.data[0]); }, [warehouses.data, wh]);

  const addByCode = async (code: string) => {
    if (!code.trim()) return;
    try {
      const r = (await api.get(`/variants/resolve?barcode=${encodeURIComponent(code.trim())}`)).data;
      const price = r.unitType === 'Koli' ? r.salePrice * r.adetEquivalent : r.salePrice;
      setCart((c) => [...c, { key: Math.random().toString(36), variantId: r.variantId, name: `${r.productName} (${r.unitType})`, unitType: r.unitType, quantity: 1, unitPrice: price }]);
      setBarcode('');
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
  };
  const add = () => addByCode(barcode);
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
    <ScrollView contentContainerStyle={{ padding: 16 }} keyboardShouldPersistTaps="handled">
      {msg ? <Text style={{ color: msg.ok ? colors.ok : '#f87171', marginBottom: 10 }}>{msg.text}</Text> : null}
      <Pressable onPress={() => setPickCust(true)}><Field label="Müşteri" editable={false} pointerEvents="none" value={customer?.name ?? ''} placeholder="Müşteri seç..." /></Pressable>
      <Text style={{ color: colors.sub, fontSize: 12, marginBottom: 8 }}>Depo: {wh?.name ?? '...'}</Text>
      <Btn title="📷 QR Okut" onPress={() => setScan(true)} />
      <View style={{ height: 8 }} />
      <Field label="veya kod yaz" value={barcode} onChangeText={setBarcode} onSubmitEditing={add} returnKeyType="done" placeholder="Ürün kodu + enter" />
      {cart.map((i) => (
        <View key={i.key} style={s.row}>
          <View style={{ flex: 1 }}>
            <Text style={s.rowMain}>{i.name}</Text>
            <Text style={s.rowSub}>{tl(i.unitPrice)} × {i.quantity} = {tl(i.unitPrice * i.quantity)}</Text>
          </View>
          <Pressable onPress={() => setCart((c) => c.map((x) => x.key === i.key ? { ...x, quantity: Math.max(1, x.quantity - 1) } : x))}><Text style={{ color: colors.accent2, fontSize: 24, paddingHorizontal: 8 }}>−</Text></Pressable>
          <Text style={{ color: colors.text, minWidth: 18, textAlign: 'center' }}>{i.quantity}</Text>
          <Pressable onPress={() => setCart((c) => c.map((x) => x.key === i.key ? { ...x, quantity: x.quantity + 1 } : x))}><Text style={{ color: colors.accent2, fontSize: 22, paddingHorizontal: 8 }}>+</Text></Pressable>
          <Pressable onPress={() => setCart((c) => c.filter((x) => x.key !== i.key))}><Text style={{ color: colors.danger, paddingLeft: 6 }}>✕</Text></Pressable>
        </View>
      ))}
      <Card style={{ marginTop: 8 }}>
        <Row left="Toplam" right={tl(total)} />
        <Btn title="Satışı Tamamla" onPress={submit} busy={busy} />
      </Card>
      <Selector visible={pickCust} title="Müşteri Seç" items={customers.data ?? []} label={(c: any) => c.name} onPick={setCustomer} onClose={() => setPickCust(false)} />
      <QrScanner visible={scan} onClose={() => setScan(false)} onScan={(code) => { setScan(false); addByCode(code); }} />
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
        refreshControl={refresh(q.refetch, q.isRefetching)}
        renderItem={({ item }: any) => (
          <Pressable onPress={() => setSel(item)}>
            <Row left={item.name} sub={item.phone ?? '-'} right={tl(item.balance)} />
          </Pressable>
        )}
        ListEmptyComponent={<Empty icon="people-outline" text="Müşteri yok" />} />
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
  const sales = useQuery({ queryKey: ['s-cari-sales', customer?.id], enabled: !!customer, queryFn: async () => (await api.get(`/sales?pageSize=10&customerId=${customer.id}`)).data.items });

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
        <View style={{ backgroundColor: colors.bg, borderTopLeftRadius: 16, borderTopRightRadius: 16, padding: 16, maxHeight: '85%' }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 12 }}>
            <Text style={s.h1}>{customer?.name}</Text>
            <Pressable onPress={onClose} hitSlop={10}><Text style={{ color: colors.sub, fontSize: 18 }}>✕</Text></Pressable>
          </View>
          <ScrollView>
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
            <Text style={s.section}>Son Satışlar</Text>
            {(sales.data ?? []).length === 0 ? <Text style={s.dim}>Satış yok</Text> :
              (sales.data ?? []).map((x: any) => (
                <Row key={x.id} left={`#${x.saleNumber}`} sub={dateStr(x.saleDate)} right={tl(x.grandTotal)} />
              ))}
          </ScrollView>
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
  const [scan, setScan] = useState(false);
  const [lines, setLines] = useState<EntryLine[]>([]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  useEffect(() => { if (warehouses.data && !wh) setWh(warehouses.data.find((w: any) => w.isDefault) ?? warehouses.data[0]); }, [warehouses.data, wh]);
  const stock = useQuery({ queryKey: ['s-stock', wh?.id], enabled: !!wh, queryFn: async () => (await api.get(`/stock/warehouse/${wh.id}?pageSize=200`)).data.items });

  const addByCode = async (code: string) => {
    if (!code.trim()) return;
    try {
      const r = (await api.get(`/variants/resolve?barcode=${encodeURIComponent(code.trim())}`)).data;
      setLines((l) => [...l, { key: Math.random().toString(36), variantId: r.variantId, name: `${r.productName} (${r.unitType})`, unitType: r.unitType, quantity: 1, unitPurchasePrice: r.salePrice ? 0 : 0 }]);
      setBarcode('');
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
  };
  const add = () => addByCode(barcode);
  const submit = async () => {
    setMsg(null);
    if (!wh || !lines.length) return setMsg({ ok: false, text: 'Satır ekleyin.' });
    try {
      await api.post('/stock/entry', { warehouseId: wh.id, idempotencyKey: Math.random().toString(36) + Date.now(), lines: lines.map((l) => ({ variantId: l.variantId, unitType: l.unitType, quantity: l.quantity, unitPurchasePrice: l.unitPurchasePrice })) });
      setMsg({ ok: true, text: 'Giriş kaydedildi.' }); setLines([]); stock.refetch();
    } catch (e) { setMsg({ ok: false, text: apiError(e) }); }
  };

  return (
    <ScrollView contentContainerStyle={{ padding: 16 }} keyboardShouldPersistTaps="handled" refreshControl={refresh(stock.refetch, stock.isRefetching)}>
      <Text style={{ color: colors.sub, fontSize: 12, marginBottom: 8 }}>Depo: {wh?.name ?? '...'}</Text>
      {msg ? <Text style={{ color: msg.ok ? colors.ok : '#f87171', marginBottom: 8 }}>{msg.text}</Text> : null}
      <Card>
        <Text style={{ color: colors.text, fontWeight: '600', marginBottom: 8 }}>Ürün Girişi</Text>
        <Btn title="📷 QR Okut" onPress={() => setScan(true)} />
        <View style={{ height: 8 }} />
        <Field value={barcode} onChangeText={setBarcode} onSubmitEditing={add} returnKeyType="done" placeholder="veya kod yaz + enter" />
        {lines.map((l) => (
          <View key={l.key} style={s.row}>
            <View style={{ flex: 1 }}><Text style={s.rowMain}>{l.name}</Text></View>
            <Field value={String(l.quantity)} onChangeText={(v) => setLines((ls) => ls.map((x) => x.key === l.key ? { ...x, quantity: Number(v) || 0 } : x))} keyboardType="numeric" style={{ width: 56, marginBottom: 0 }} />
            <Field value={String(l.unitPurchasePrice)} onChangeText={(v) => setLines((ls) => ls.map((x) => x.key === l.key ? { ...x, unitPurchasePrice: Number(v) || 0 } : x))} keyboardType="numeric" style={{ width: 72, marginBottom: 0, marginLeft: 6 }} />
          </View>
        ))}
        <Btn title="Girişi Kaydet" onPress={submit} />
      </Card>
      <Text style={s.section}>Depo Stoğu</Text>
      {(stock.data ?? []).length === 0 ? <Text style={s.dim}>Stok yok</Text> :
        (stock.data ?? []).map((x: any) => (
          <Row key={x.variantId} left={x.productName} sub={x.adetBarcode} right={`${x.quantity}`} />
        ))}
      <QrScanner visible={scan} onClose={() => setScan(false)} onScan={(code) => { setScan(false); addByCode(code); }} />
    </ScrollView>
  );
}
