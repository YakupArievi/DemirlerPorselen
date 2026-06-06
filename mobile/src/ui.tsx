import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View, type TextInputProps } from 'react-native';

export const colors = {
  bg: '#0f172a', card: '#1e293b', card2: '#273449', line: '#334155',
  text: '#f1f5f9', sub: '#94a3b8', muted: '#64748b',
  accent: '#0284c7', accent2: '#38bdf8', ok: '#059669', warn: '#d97706', danger: '#dc2626',
};

export function Btn({ title, onPress, busy, kind = 'primary', disabled }: {
  title: string; onPress: () => void; busy?: boolean; kind?: 'primary' | 'ghost' | 'danger'; disabled?: boolean;
}) {
  const bg = kind === 'primary' ? colors.accent : kind === 'danger' ? colors.danger : 'transparent';
  return (
    <Pressable onPress={onPress} disabled={busy || disabled}
      style={[s.btn, { backgroundColor: bg, borderWidth: kind === 'ghost' ? 1 : 0, borderColor: colors.line, opacity: (busy || disabled) ? 0.6 : 1 }]}>
      {busy ? <ActivityIndicator color="#fff" /> : <Text style={[s.btnText, kind === 'ghost' && { color: colors.sub }]}>{title}</Text>}
    </Pressable>
  );
}

export function Field(props: TextInputProps & { label?: string }) {
  const { label, ...rest } = props;
  return (
    <View style={{ marginBottom: 12 }}>
      {label ? <Text style={s.label}>{label}</Text> : null}
      <TextInput placeholderTextColor={colors.muted} {...rest} style={[s.input, props.style]} />
    </View>
  );
}

export function Card({ children, style }: { children: React.ReactNode; style?: any }) {
  return <View style={[s.card, style]}>{children}</View>;
}

export function Row({ left, right, sub }: { left: string; right?: string; sub?: string }) {
  return (
    <View style={s.row}>
      <View style={{ flex: 1 }}>
        <Text style={s.rowMain}>{left}</Text>
        {sub ? <Text style={s.rowSub}>{sub}</Text> : null}
      </View>
      {right ? <Text style={s.rowRight}>{right}</Text> : null}
    </View>
  );
}

export function TabBar({ tabs, active, onChange }: { tabs: { key: string; label: string }[]; active: string; onChange: (k: string) => void }) {
  return (
    <View style={s.tabs}>
      {tabs.map((t) => (
        <Pressable key={t.key} onPress={() => onChange(t.key)} style={[s.tab, active === t.key && { backgroundColor: colors.accent }]}>
          <Text style={[s.tabText, active === t.key && { color: '#fff' }]}>{t.label}</Text>
        </Pressable>
      ))}
    </View>
  );
}

export function Center({ children }: { children: React.ReactNode }) {
  return <View style={[s.fill, { justifyContent: 'center', alignItems: 'center' }]}>{children}</View>;
}

export const s = StyleSheet.create({
  fill: { flex: 1, backgroundColor: colors.bg },
  btn: { borderRadius: 10, padding: 14, alignItems: 'center', marginTop: 4 },
  btnText: { color: '#fff', fontWeight: '700', fontSize: 15 },
  label: { color: colors.sub, fontSize: 13, marginBottom: 6 },
  input: { backgroundColor: colors.card, color: colors.text, borderRadius: 10, padding: 13, borderWidth: 1, borderColor: colors.line },
  card: { backgroundColor: colors.card, borderRadius: 12, padding: 16, marginBottom: 12 },
  row: { flexDirection: 'row', alignItems: 'center', backgroundColor: colors.card, padding: 14, borderRadius: 10, marginBottom: 8 },
  rowMain: { color: colors.text, fontWeight: '600' },
  rowSub: { color: colors.sub, fontSize: 12, marginTop: 2 },
  rowRight: { color: colors.text, fontWeight: '700', marginLeft: 8 },
  tabs: { flexDirection: 'row', backgroundColor: colors.card, margin: 12, borderRadius: 10, padding: 4 },
  tab: { flex: 1, paddingVertical: 9, borderRadius: 8, alignItems: 'center' },
  tabText: { color: colors.sub, fontWeight: '600', fontSize: 13 },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingTop: 14, paddingBottom: 4 },
  h1: { color: colors.text, fontSize: 22, fontWeight: 'bold' },
});
