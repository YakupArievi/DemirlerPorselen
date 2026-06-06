using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Reporting.Reports;

public interface IReportService
{
    Task<ProfitabilityReportDto> GetProfitabilityAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<StockReportDto> GetStockReportAsync(Guid? warehouseId, bool onlyCritical, CancellationToken ct = default);
    Task<Result<CustomerSummaryDto>> GetCustomerSummaryAsync(Guid customerId, CancellationToken ct = default);
}

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _time;

    public ReportService(IApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<ProfitabilityReportDto> GetProfitabilityAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var fromDate = from ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = to ?? now;

        var lines = await _db.SaleLines.AsNoTracking()
            .Where(l => l.Sale.Status == SaleStatus.Active && l.Sale.SaleDate >= fromDate && l.Sale.SaleDate <= toDate)
            .Select(l => new
            {
                ProductName = l.Variant.Product.Name,
                CategoryName = l.Variant.Product.Category.Name,
                Revenue = l.LineTotal,
                Cost = l.UnitCost * l.AdetQuantity
            })
            .ToListAsync(ct);

        var byProduct = lines
            .GroupBy(l => l.ProductName)
            .Select(g => BuildRow(g.Key, g.Sum(x => x.Revenue), g.Sum(x => x.Cost)))
            .OrderByDescending(r => r.Profit)
            .ToList();

        var byCategory = lines
            .GroupBy(l => l.CategoryName)
            .Select(g => BuildRow(g.Key, g.Sum(x => x.Revenue), g.Sum(x => x.Cost)))
            .OrderByDescending(r => r.Profit)
            .ToList();

        var totalRevenue = lines.Sum(x => x.Revenue);
        var totalCost = lines.Sum(x => x.Cost);
        var totalProfit = totalRevenue - totalCost;

        return new ProfitabilityReportDto(
            fromDate, toDate, totalRevenue, totalCost, totalProfit,
            Margin(totalProfit, totalRevenue), Markup(totalProfit, totalCost),
            byProduct, byCategory);
    }

    public async Task<StockReportDto> GetStockReportAsync(Guid? warehouseId, bool onlyCritical, CancellationToken ct = default)
    {
        var q = _db.StockItems.AsNoTracking().AsQueryable();
        if (warehouseId is { } wid)
            q = q.Where(s => s.WarehouseId == wid);

        var rows = await q
            .OrderBy(s => s.Variant.Product.Name)
            .Select(s => new StockReportRowDto(
                s.Variant.Product.Name, s.Variant.Color, s.Variant.Size, s.Variant.AdetBarcode,
                s.Warehouse.Name, s.Quantity, s.Variant.MinStock, s.Quantity < s.Variant.MinStock))
            .ToListAsync(ct);

        if (onlyCritical)
            rows = rows.Where(r => r.IsBelowMin).ToList();

        string? warehouseName = null;
        if (warehouseId is { } id)
            warehouseName = await _db.Warehouses.AsNoTracking().Where(w => w.Id == id).Select(w => w.Name).FirstOrDefaultAsync(ct);

        return new StockReportDto(
            _time.GetUtcNow().UtcDateTime, warehouseName,
            rows.Count, rows.Count(r => r.IsBelowMin), rows);
    }

    public async Task<Result<CustomerSummaryDto>> GetCustomerSummaryAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Id, c.Name, c.Phone, c.OpeningBalance, c.Balance })
            .FirstOrDefaultAsync(ct);

        if (customer is null)
            return Result.Failure<CustomerSummaryDto>(Error.NotFound("Müşteri bulunamadı."));

        var totalSales = await _db.Sales.AsNoTracking()
            .Where(s => s.CustomerId == customerId && s.Status == SaleStatus.Active)
            .SumAsync(s => (decimal?)s.GrandTotal, ct) ?? 0m;
        var saleCount = await _db.Sales.AsNoTracking().CountAsync(s => s.CustomerId == customerId && s.Status == SaleStatus.Active, ct);
        var totalCollections = await _db.Payments.AsNoTracking()
            .Where(p => p.CustomerId == customerId).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        return Result.Success(new CustomerSummaryDto(
            customer.Id, customer.Name, customer.Phone, customer.OpeningBalance, customer.Balance,
            totalSales, totalCollections, saleCount, _time.GetUtcNow().UtcDateTime));
    }

    private static ProfitRowDto BuildRow(string name, decimal revenue, decimal cost)
    {
        var profit = revenue - cost;
        return new ProfitRowDto(name, revenue, cost, profit, Margin(profit, revenue), Markup(profit, cost));
    }

    // Marj = Kar / Satış × 100
    private static decimal Margin(decimal profit, decimal revenue)
        => revenue == 0 ? 0 : Math.Round(profit / revenue * 100, 2);

    // Markup = Kar / Alış × 100
    private static decimal Markup(decimal profit, decimal cost)
        => cost == 0 ? 0 : Math.Round(profit / cost * 100, 2);
}
