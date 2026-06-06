# Toptancı Mobil (Faz 7 — İskelet)

Müşterilerin kendi cari durumlarını görebileceği React Native (Expo) uygulamasının iskeleti.
**Gelecek faz** olduğundan yalnızca temel akış kurulmuştur; mevcut backend API'lerini kullanır.

## Kapsam (hedef)
- Giriş (JWT) — `/api/auth/login`
- Borç / cari bakiye görüntüleme — `/api/customers/{id}/balance`
- Cari ekstre — `/api/customers/{id}/statement` ve PDF indirme — `/api/reports/customers/{id}/statement.pdf`
- Alınan ürünler (satış geçmişi) — `/api/sales?customerId=...`

## Çalıştırma
```bash
cd mobile
npm install
npm start   # Expo; Android emülatörü için API_BASE = http://10.0.2.2:5080
```

> Not: `src/api.ts` içindeki `API_BASE` adresini kendi ağ/host IP'nize göre güncelleyin.
> Gerçek senaryoda müşteri rolü ve kendi kaydına kısıtlama (kullanıcı↔müşteri eşlemesi) eklenmelidir.
