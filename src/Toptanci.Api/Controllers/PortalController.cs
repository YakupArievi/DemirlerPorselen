using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Api.Extensions;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Portal;
using Toptanci.Application.Features.Sales.Accounts;
using Toptanci.Application.Features.Sales.Sales;

namespace Toptanci.Api.Controllers;

/// <summary>
/// Mobil müşteri portalı veri uçları. Her uç YALNIZCA token'daki müşteriye ait veriyi döndürür.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.Portal)]
[Route("api/portal/me")]
public sealed class PortalController : ControllerBase
{
    private readonly IAccountService _account;
    private readonly ISaleService _sales;
    private readonly IReportPdfService _pdf;
    private readonly IPortalAuthService _auth;

    public PortalController(IAccountService account, ISaleService sales, IReportPdfService pdf, IPortalAuthService auth)
    {
        _account = account;
        _sales = sales;
        _pdf = pdf;
        _auth = auth;
    }

    private Guid CustomerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("balance")]
    public async Task<IActionResult> Balance(CancellationToken ct)
        => (await _account.GetBalanceAsync(CustomerId, ct)).ToActionResult();

    [HttpGet("statement")]
    public async Task<IActionResult> Statement([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => (await _account.GetStatementAsync(CustomerId, from, to, ct)).ToActionResult();

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sales.GetAllAsync(new SaleQuery { CustomerId = CustomerId, Page = page, PageSize = pageSize }, ct));

    /// <summary>Satış detayı — yalnızca bu müşteriye aitse döner.</summary>
    [HttpGet("sales/{id:guid}")]
    public async Task<IActionResult> SaleDetail(Guid id, CancellationToken ct)
    {
        var result = await _sales.GetByIdAsync(id, ct);
        if (result.IsFailure || result.Value.CustomerId != CustomerId)
            return NotFound(new { message = "Satış bulunamadı." });
        return Ok(result.Value);
    }

    [HttpGet("statement.pdf")]
    public async Task<IActionResult> StatementPdf([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _account.GetStatementAsync(CustomerId, from, to, ct);
        if (result.IsFailure) return NotFound(new { result.Error.Message });
        return File(_pdf.AccountStatement(result.Value), "application/pdf", "ekstre.pdf");
    }

    /// <summary>Müşteri kendi parolasını değiştirir.</summary>
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePortalPasswordRequest request, CancellationToken ct)
        => (await _auth.ChangePasswordAsync(CustomerId, request, ct)).ToActionResult();
}
