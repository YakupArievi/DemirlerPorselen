import { useState } from 'react';
import { Image, Pressable, SafeAreaView, Text, View } from 'react-native';
import { apiError, portalLogin, staffLogin, type Session } from '../api';
import { Btn, Field, colors, s } from '../ui';

export function Login({ onLogin }: { onLogin: (sess: Session) => void }) {
  const [mode, setMode] = useState<'staff' | 'portal'>('portal');
  const [u, setU] = useState('');
  const [p, setP] = useState('');
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    setErr(''); setBusy(true);
    try {
      const sess = mode === 'staff' ? await staffLogin(u.trim(), p) : await portalLogin(u.trim(), p);
      onLogin(sess);
    } catch (e) { setErr(apiError(e)); }
    finally { setBusy(false); }
  };

  return (
    <SafeAreaView style={s.fill}>
      <View style={{ flex: 1, justifyContent: 'center', padding: 24 }}>
        <Image source={require('../../assets/icon.png')} style={{ width: 200, height: 200, borderRadius: 24, alignSelf: 'center', marginBottom: 8 }} resizeMode="contain" />
        <Text style={{ color: colors.sub, textAlign: 'center', marginBottom: 24 }}>Stok & Cari</Text>

        <View style={{ flexDirection: 'row', backgroundColor: colors.card, borderRadius: 10, padding: 4, marginBottom: 18 }}>
          {(['portal', 'staff'] as const).map((m) => (
            <Pressable key={m} onPress={() => { setMode(m); setErr(''); }}
              style={{ flex: 1, paddingVertical: 10, borderRadius: 8, alignItems: 'center', backgroundColor: mode === m ? colors.accent : 'transparent' }}>
              <Text style={{ color: mode === m ? '#fff' : colors.sub, fontWeight: '600' }}>
                {m === 'portal' ? 'Müşteri' : 'Personel'}
              </Text>
            </Pressable>
          ))}
        </View>

        {err ? <Text style={{ color: '#f87171', textAlign: 'center', marginBottom: 10 }}>{err}</Text> : null}

        <Field
          label={mode === 'staff' ? 'Kullanıcı adı' : 'Telefon'}
          value={u} onChangeText={setU} autoCapitalize="none"
          keyboardType={mode === 'staff' ? 'default' : 'phone-pad'}
          placeholder={mode === 'staff' ? 'admin' : '5xx...'}
        />
        <Field label="Parola" value={p} onChangeText={setP} secureTextEntry placeholder="••••••" />
        <Btn title="Giriş" onPress={submit} busy={busy} />
      </View>
    </SafeAreaView>
  );
}
