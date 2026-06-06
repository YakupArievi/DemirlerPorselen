using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Application.Features.Forecasting;

namespace Toptanci.Api.Controllers;

[Authorize(Policy = Policies.PatronOrAdmin)]
public sealed class ForecastController : ApiControllerBase
{
    private readonly IForecastService _service;

    public ForecastController(IForecastService service) => _service = service;

    /// <summary>Geçmiş satışa göre sipariş önerisi ve kritik stok erken uyarısı.</summary>
    [HttpGet("reorder")]
    public async Task<IActionResult> Reorder([FromQuery] int lookbackDays = 30, [FromQuery] int horizonDays = 14, CancellationToken ct = default)
        => Ok(await _service.GetReorderSuggestionsAsync(lookbackDays, horizonDays, ct));

    /// <summary>Tek varyant için basit satış tahmini.</summary>
    [HttpGet("variant/{variantId:guid}")]
    public async Task<IActionResult> Variant(Guid variantId, [FromQuery] int lookbackDays = 30, [FromQuery] int horizonDays = 14, CancellationToken ct = default)
        => Ok(await _service.GetSalesForecastAsync(variantId, lookbackDays, horizonDays, ct));
}
