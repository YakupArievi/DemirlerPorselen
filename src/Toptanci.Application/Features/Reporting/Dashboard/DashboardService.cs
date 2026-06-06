using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Reporting.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetSummaryAsync(CancellationToken ct = default);
}

public sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _time;

    public DashboardService(IApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<DashboardDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeSales = _db.Sales.AsNoTracking().Where(s => s.Status == SaleStatus.Active);

        var todayRevenue = await activeSales.Where(s => s.SaleDate >= todayStart).SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0m;
        var monthRevenue = await activeSales.Where(s => s.SaleDate >= monthStart).SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0m;

        var todayCollections = await _db.Payments.AsNoTracking().Where(p => p.PaymentDate >= todayStart).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var monthCollections = await _db.Payments.AsNoTracking().Where(p => p.PaymentDate >= monthStart).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var totalReceivables = await _db.Customers.AsNoTracking().Where(c => c.Balance > 0).SumAsync(c => (decimal?)c.Balance, ct) ?? 0m;

        // Kritik stok: varyant toplam stoğu < minStock
        var criticalAll = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.IsActive)
            .Select(v => new
            {
                v.Id,
                ProductName = v.Product.Name,
                v.Color,
                v.Size,
                v.MinStock,
                Total = v.StockItems.Sum(s => (int?)s.Quantity) ?? 0
            })
            .Where(x => x.Total < x.MinStock)
            .ToListAsync(ct);

        var criticalStocks = criticalAll
            .OrderBy(x => x.Total)
            .Take(20)
            .Select(x => new CriticalStockDto(x.Id, x.ProductName, x.Color, x.Size, x.Total, x.MinStock))
            .ToList();

        // En çok satan (bu ay): VariantId ile grupla (çevrilebilir), isimleri ayrı çek
        var topGrouped = await _db.SaleLines.AsNoTracking()
            .Where(l => l.Sale.Status == SaleStatus.Active && l.Sale.SaleDate >= monthStart)
            .GroupBy(l => l.VariantId)
            .Select(g => new { VariantId = g.Key, Qty = g.Sum(x => x.AdetQuantity), Revenue = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Qty)
            .Take(10)
            .ToListAsync(ct);

        var topIds = topGrouped.Select(x => x.VariantId).ToList();
        var variantInfo = await _db.ProductVariants.AsNoTracking()
            .Where(v => topIds.Contains(v.Id))
            .Select(v => new { v.Id, ProductName = v.Product.Name, v.Color, v.Size })
            .ToDictionaryAsync(v => v.Id, ct);

        var topProducts = topGrouped
            .Where(x => variantInfo.ContainsKey(x.VariantId))
            .Select(x =>
            {
                var info = variantInfo[x.VariantId];
                return new TopProductDto(x.VariantId, info.ProductName, info.Color, info.Size, x.Qty, x.Revenue);
            })
            .ToList();

        var recentSales = await _db.Sales.AsNoTracking()
            .OrderByDescending(s => s.SaleNumber)
            .Take(10)
            .Select(s => new RecentSaleDto(s.Id, s.SaleNumber, s.Customer.Name, s.SaleDate, s.GrandTotal))
            .ToListAsync(ct);

        var brokenCount = await _db.BrokenProductRecords.AsNoTracking().CountAsync(b => b.RecordDate >= monthStart, ct);
        var brokenQty = await _db.BrokenProductRecords.AsNoTracking().Where(b => b.RecordDate >= monthStart).SumAsync(b => (int?)b.Quantity, ct) ?? 0;

        return new DashboardDto(
            todayRevenue, monthRevenue, todayCollections, monthCollections, totalReceivables,
            criticalAll.Count, brokenCount, brokenQty,
            criticalStocks, topProducts, recentSales);
    }
}
