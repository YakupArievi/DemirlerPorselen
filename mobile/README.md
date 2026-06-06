# Toptancı Müşteri Mobil (Faz 7)

Müşterilerin kendi cari durumlarını gördüğü React Native (Expo) uygulaması.
Müşteri **telefon + parola** ile giriş yapar ve **yalnızca kendi** verisini görür.

## Özellikler
- Giriş / oturum (token + otomatik refresh, AsyncStorage)
- **Borç:** güncel bakiye — `/api/portal/me/balance`
- **Ekstre:** dönem hareketleri (borç/alacak/bakiye) — `/api/portal/me/statement`
- **Aldıklarım:** satış/fiş geçmişi — `/api/portal/me/sales`
- Çıkış (refresh token iptali)

## Güvenlik
- Portal token'ları **ayrı JWT şeması + farklı audience** ile üretilir; personel API uçlarına erişemez.
- "me" uçları token'daki müşteri kimliğine sabitlidir (başka müşterinin verisi görülemez).

## Müşteriye giriş bilgisi tanımlama (personel tarafı)
API ile:
```
POST /api/customers/{id}/portal   (Patron/Admin)
{ "phone": "5559998877", "password": "1234", "enabled": true }
```

## Çalıştırma
```bash
cd mobile
npm install
npm start          # Expo Dev Tools; a -> Android, i -> iOS
```
> `src/api.ts` içindeki `API_BASE`'i ortamınıza göre ayarlayın:
> Android emülatör `http://10.0.2.2:5080`, gerçek cihaz `http://<LAN-IP>:5080`.
