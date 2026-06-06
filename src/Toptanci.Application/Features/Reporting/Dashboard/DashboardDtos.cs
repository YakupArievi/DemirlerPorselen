namespace Toptanci.Application.Features.Reporting.Dashboard;

public sealed record CriticalStockDto(Guid VariantId, string ProductName, string? Color, string? Size, int TotalQuantity, int MinStock);

public sealed record TopProductDto(Guid VariantId, string ProductName, string? Color, string? Size, int SoldQuantity, decimal Revenue);

public sealed record RecentSaleDto(Guid Id, long SaleNumber, string CustomerName, DateTime SaleDate, decimal GrandTotal);

public sealed record DashboardDto(
    decimal TodayRevenue,
    decimal MonthRevenue,
    decimal TodayCollections,
    decimal MonthCollections,
    decimal TotalReceivables,
    int CriticalStockCount,
    int BrokenCountThisMonth,
    int BrokenQuantityThisMonth,
    IReadOnlyList<CriticalStockDto> CriticalStocks,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<RecentSaleDto> RecentSales);
