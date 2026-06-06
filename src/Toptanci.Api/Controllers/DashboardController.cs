using Microsoft.AspNetCore.Mvc;
using Toptanci.Application.Features.Reporting.Dashboard;

namespace Toptanci.Api.Controllers;

public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
        => Ok(await _service.GetSummaryAsync(ct));
}
