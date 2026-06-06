using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Application.Features.Reporting.Dashboard;

namespace Toptanci.Api.Controllers;

[Authorize(Policy = Policies.PatronOrAdmin)] // Ciro/alacak icerir -> Depocu erisemez
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
        => Ok(await _service.GetSummaryAsync(ct));
}
