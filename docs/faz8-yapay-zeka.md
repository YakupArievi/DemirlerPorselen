# Faz 8 — Yapay Zeka Modülü (Tasarım + v1)

Geçmiş satış verisinden stok tahmini, sipariş önerisi, çok satan analizi ve kritik stok
erken uyarısı. v1 basit istatistiksel yöntemlerle (hareketli ortalama) çalışır; arayüzler
(`IForecastService`) sabit kalacak şekilde ileride ML modeliyle değiştirilebilir.

## Mevcut (v1 — uygulandı)
- **Sipariş önerisi / erken uyarı:** `GET /api/forecast/reorder?lookbackDays=30&horizonDays=14`
  - Varyant başına: ortalama günlük satış, tahmini stok-bitiş süresi, önerilen sipariş adedi,
    aciliyet (Yüksek/Orta/Düşük). Min stok altı veya satış hızına göre sıralı.
- **Tek varyant tahmini:** `GET /api/forecast/variant/{id}` — hareketli ortalama ile horizon talebi.
- **Çok satan analizi:** Dashboard `topProducts` (aylık) — `GET /api/dashboard/summary`.

### Hesap (v1)
- `avgDaily = lookback dönemindeki toplam satılan adet / lookbackDays`
- `daysOfStockLeft = currentStock / avgDaily`
- `suggestedOrderQty = ceil(avgDaily * horizonDays + minStock - currentStock)`

## Yol haritası (v2+)
1. **Mevsimsellik & trend:** Holt-Winters / üssel düzleştirme; haftanın günü etkisi.
2. **ML modeli:** ML.NET veya harici servis; özellikler: geçmiş satış, fiyat, kampanya, sezon.
3. **Tedarik süresi (lead time):** sipariş önerisine tedarikçi teslim süresini dahil et.
4. **Servis sınırı:** `IForecastService` korunur; implementasyon `Infrastructure`'da değişir.
5. **Veri hattı:** ağır sorgular için ayrı okuma modeli / Dapper / materyalize görünüm.

## Entegrasyon
- Frontend: Panel'e "Sipariş Önerileri" kartı/sayfası (Patron/Admin) eklenebilir.
- Yetki: tüm tahmin uçları `PatronOrAdmin` politikası ile korunur.
