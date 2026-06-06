using Toptanci.Application.Features.Reporting.Reports;
using Toptanci.Application.Features.Sales.Accounts;

namespace Toptanci.Application.Common.Abstractions;

/// <summary>Rapor DTO'larını PDF (byte[]) olarak üretir.</summary>
public interface IReportPdfService
{
    byte[] AccountStatement(AccountStatementDto statement);
    byte[] CustomerSummary(CustomerSummaryDto summary);
    byte[] StockReport(StockReportDto report);
    byte[] Profitability(ProfitabilityReportDto report);
}
