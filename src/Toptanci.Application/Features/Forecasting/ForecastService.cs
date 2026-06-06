using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Forecasting;

public sealed record ReorderSuggestionDto(
    Guid VariantId, string ProductName, string? Color, string? Size,
    int CurrentStock, double AvgDailySales, double DaysOfStockLeft,
    int MinStock, int SuggestedOrderQty, string Urgency);

public sealed record SalesForecastDto(
    Guid VariantId, int LookbackDays, double AvgDailySales, int HorizonDays, double PredictedDemand);

/// <summary>
/// Geçmiş satış verisinden basit tahmin (hareketli ortalama). v1: sipariş önerisi + erken uyarı.
/// İleride mevsimsellik/ML modeli ile değiştirilebilir; arayüz sabit kalır.
/// </summary>
public interface IForecastService
{
    Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(int lookbackDays, int horizonDays, CancellationToken ct = default);
    Task<SalesForecastDto> GetSalesForecastAsync(Guid variantId, int lookbackDays, int horizonDays, CancellationToken ct = default);
}

public sealed class ForecastService : IForecastService
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _time;

    public ForecastService(IApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(int lookbackDays, int horizonDays, CancellationToken ct = default)
    {
        lookbackDays = Math.Clamp(lookbackDays, 1, 365);
        horizonDays = Math.Clamp(horizonDays, 1, 180);
        var since = _time.GetUtcNow().UtcDateTime.AddDays(-lookbackDays);

        // Dönemdeki satışları varyant bazında topla
        var sold = await _db.SaleLines.AsNoTracking()
            .Where(l => l.Sale.Status == SaleStatus.Active && l.Sale.SaleDate >= since)
            .GroupBy(l => l.VariantId)
            .Select(g => new { VariantId = g.Key, Total = g.Sum(x => x.AdetQuantity) })
            .ToDictionaryAsync(x => x.VariantId, x => x.Total, ct);

        // Aktif varyantlar + güncel stok + min
        var variants = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.IsActive)
            .Select(v => new
            {
                v.Id, ProductName = v.Product.Name, v.Color, v.Size, v.MinStock,
                Stock = v.StockItems.Sum(s => (int?)s.Quantity) ?? 0
            })
            .ToListAsync(ct);

        var result = new List<ReorderSuggestionDto>();
        foreach (var v in variants)
        {
            var totalSold = sold.GetValueOrDefault(v.Id, 0);
            var avgDaily = (double)totalSold / lookbackDays;
            var daysLeft = avgDaily > 0 ? v.Stock / avgDaily : double.PositiveInfinity;

            // Horizon + emniyet (min stok) kadarını karşılayacak öneri
            var target = avgDaily * horizonDays + v.MinStock;
            var suggested = (int)Math.Max(0, Math.Ceiling(target - v.Stock));

            // Sadece anlamlı olanları döndür (satışı olan veya min altı)
            if (avgDaily <= 0 && v.Stock >= v.MinStock)
                continue;

            var urgency =
                v.Stock < v.MinStock || daysLeft < horizonDays / 2.0 ? "Yüksek" :
                daysLeft < horizonDays ? "Orta" : "Düşük";

            result.Add(new ReorderSuggestionDto(
                v.Id, v.ProductName, v.Color, v.Size, v.Stock,
                Math.Round(avgDaily, 2), double.IsInfinity(daysLeft) ? -1 : Math.Round(daysLeft, 1),
                v.MinStock, suggested, urgency));
        }

        // En aciller önce
        return result
            .OrderBy(r => r.Urgency == "Yüksek" ? 0 : r.Urgency == "Orta" ? 1 : 2)
            .ThenBy(r => r.DaysOfStockLeft < 0 ? double.MaxValue : r.DaysOfStockLeft)
            .ToList();
    }

    public async Task<SalesForecastDto> GetSalesForecastAsync(Guid variantId, int lookbackDays, int horizonDays, CancellationToken ct = default)
    {
        lookbackDays = Math.Clamp(lookbackDays, 1, 365);
        horizonDays = Math.Clamp(horizonDays, 1, 180);
        var since = _time.GetUtcNow().UtcDateTime.AddDays(-lookbackDays);

        var totalSold = await _db.SaleLines.AsNoTracking()
            .Where(l => l.VariantId == variantId && l.Sale.Status == SaleStatus.Active && l.Sale.SaleDate >= since)
            .SumAsync(l => (int?)l.AdetQuantity, ct) ?? 0;

        var avgDaily = (double)totalSold / lookbackDays;
        return new SalesForecastDto(variantId, lookbackDays, Math.Round(avgDaily, 2), horizonDays, Math.Round(avgDaily * horizonDays, 1));
    }
}
