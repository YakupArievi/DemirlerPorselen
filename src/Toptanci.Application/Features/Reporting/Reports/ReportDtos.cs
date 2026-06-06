namespace Toptanci.Application.Features.Reporting.Reports;

public sealed record ProfitRowDto(
    string Name, decimal Revenue, decimal Cost, decimal Profit, decimal MarginPercent, decimal MarkupPercent);

public sealed record ProfitabilityReportDto(
    DateTime FromDate, DateTime ToDate,
    decimal TotalRevenue, decimal TotalCost, decimal TotalProfit,
    decimal MarginPercent, decimal MarkupPercent,
    IReadOnlyList<ProfitRowDto> ByProduct,
    IReadOnlyList<ProfitRowDto> ByCategory);

public sealed record StockReportRowDto(
    string ProductName, string? Color, string? Size, string AdetBarcode,
    string WarehouseName, int Quantity, int MinStock, bool IsBelowMin);

public sealed record StockReportDto(
    DateTime GeneratedAt, string? WarehouseName, int TotalLines, int CriticalLines,
    IReadOnlyList<StockReportRowDto> Rows);

public sealed record CustomerSummaryDto(
    Guid CustomerId, string Name, string? Phone,
    decimal OpeningBalance, decimal Balance,
    decimal TotalSales, decimal TotalCollections, int SaleCount, DateTime GeneratedAt);
