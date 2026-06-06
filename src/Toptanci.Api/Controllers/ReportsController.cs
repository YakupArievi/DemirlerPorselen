using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Reporting.Reports;
using Toptanci.Application.Features.Sales.Accounts;

namespace Toptanci.Api.Controllers;

public sealed class ReportsController : ApiControllerBase
{
    private readonly IReportService _reports;
    private readonly IAccountService _account;
    private readonly IReportPdfService _pdf;

    public ReportsController(IReportService reports, IAccountService account, IReportPdfService pdf)
    {
        _reports = reports;
        _account = account;
        _pdf = pdf;
    }

    // ---- JSON ----

    [HttpGet("profitability")]
    [Authorize(Policy = Policies.PatronOrAdmin)]
    public async Task<IActionResult> Profitability([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.GetProfitabilityAsync(from, to, ct));

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] Guid? warehouseId, [FromQuery] bool onlyCritical, CancellationToken ct)
        => Ok(await _reports.GetStockReportAsync(warehouseId, onlyCritical, ct));

    // ---- PDF ----

    [HttpGet("customers/{id:guid}/statement.pdf")]
    public async Task<IActionResult> StatementPdf(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _account.GetStatementAsync(id, from, to, ct);
        if (result.IsFailure) return NotFound(new { result.Error.Message });
        return File(_pdf.AccountStatement(result.Value), "application/pdf", $"ekstre-{id}.pdf");
    }

    [HttpGet("customers/{id:guid}/summary.pdf")]
    public async Task<IActionResult> CustomerSummaryPdf(Guid id, CancellationToken ct)
    {
        var result = await _reports.GetCustomerSummaryAsync(id, ct);
        if (result.IsFailure) return NotFound(new { result.Error.Message });
        return File(_pdf.CustomerSummary(result.Value), "application/pdf", $"musteri-ozet-{id}.pdf");
    }

    [HttpGet("stock.pdf")]
    public async Task<IActionResult> StockPdf([FromQuery] Guid? warehouseId, [FromQuery] bool onlyCritical, CancellationToken ct)
    {
        var report = await _reports.GetStockReportAsync(warehouseId, onlyCritical, ct);
        return File(_pdf.StockReport(report), "application/pdf", "stok-raporu.pdf");
    }

    [HttpGet("profitability.pdf")]
    [Authorize(Policy = Policies.PatronOrAdmin)]
    public async Task<IActionResult> ProfitabilityPdf([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var report = await _reports.GetProfitabilityAsync(from, to, ct);
        return File(_pdf.Profitability(report), "application/pdf", "karlilik-raporu.pdf");
    }
}
