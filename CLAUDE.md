# Toptancı Stok & Cari Yönetim Sistemi

Toptan satış yapan bir işletme için stok, satış, cari hesap ve raporlama sistemi.
Backend: ASP.NET Core (.NET 10) Web API + EF Core + SQL Server. Frontend: React PWA (offline-first).

## Mimari

Katmanlı (Clean Architecture benzeri) yapı, `src/` altında:

- **Toptanci.Domain** — Entity'ler, enum'lar, domain kuralları. Hiçbir katmana referansı yok.
- **Toptanci.Application** — Use-case'ler, DTO'lar, servis arayüzleri, validator'lar, mapping. → Domain
- **Toptanci.Infrastructure** — EF Core DbContext, repository'ler, dış servis implementasyonları. → Application
- **Toptanci.Api** — Controller'lar, DI kompozisyonu, middleware. → Application, Infrastructure

Her katmanda bir `DependencyInjection` extension'ı var (`AddApplication`, `AddInfrastructure`).
Program.cs sadece bu extension'ları çağırır (composition root).

## Değişmez Mimari Kuralları

Bu kurallar tüm fazlar boyunca geçerlidir. Yeni kod yazarken bunlara uy:

1. **Primary key = `Guid`** — offline'da client tarafında kayıt üretilebilmesi için zorunlu.
2. **Stok temel birimi = `adet`** — Koli = `KoliIciAdet × adet`. Tüm stok hareketleri adete normalize edilir.
3. **Soft delete** — Hiçbir kayıt fiziksel silinmez; `IsDeleted` ile işaretlenir. Global query filter ile gizlenir.
4. **Audit** — Tüm entity'lerde `CreatedBy/CreatedAt/ModifiedBy/ModifiedAt`. SaveChanges interceptor ile otomatik doldurulur.
5. **Optimistic concurrency** — Tüm entity'lerde `RowVersion`.
6. **Stok = ledger** — Stok her zaman değişmez (append-only) `StockMovement` kayıtlarından türetilebilir.
   `StockItem` yalnızca bir cache'tir; hareketle aynı transaction içinde güncellenir.
7. **Cari = ledger** — Müşteri bakiyesi `AccountTransaction` hareketlerinden türetilebilir; bakiye cache'lenir.
8. **İptal/iade = storno** — Orijinal kayıt SİLİNMEZ; tersleyen hareket üretilir. İşlemler idempotent (`IdempotencyKey`).
9. **Fiyat & maliyet snapshot** — Satış satırında birim fiyat ve maliyet o anki değerden snapshot'lanır;
   sonradan fiyat değişse bile eski satışlar değişmez.
10. **Maliyet yöntemi** — Ağırlıklı ortalama alış maliyeti.
11. **Para birimi** — Yalnızca TRY. **KDV** v1'de yok (fiyatlar net).
12. **Stok/fiyat/barkod VARYANT seviyesinde** tutulur (Product değil, ProductVariant).

## Build & Çalıştırma

```powershell
dotnet build Toptanci.slnx
dotnet run --project src/Toptanci.Api
```

> Not: Solution dosyası .NET 10'un yeni XML formatında: `Toptanci.slnx`.

## Yol Haritası (Fazlar)

- **Faz 0** — İskelet (✅ 0.1 tamam), ortak altyapı (0.2), auth (0.3)
- **Faz 1** — Ürün & stok çekirdeği (varyant, barkod, depo, ledger, ürün girişi)
- **Faz 2** — Satış & cari (müşteri, cari hesap, satış, ödeme, iptal/iade, fiyat geçmişi)
- **Faz 3** — Kırık ürün, stok sayımı, depo transferi
- **Faz 4** — Raporlama (dashboard, PDF)
- **Faz 5** — Frontend (React PWA)
- **Faz 6** — Offline & senkronizasyon (en kritik)
- **Faz 7** — Mobil (React Native)
- **Faz 8** — Yapay zeka (stok tahmini)
