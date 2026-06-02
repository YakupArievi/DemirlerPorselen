using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Operations.Counts;

namespace Toptanci.Api.Controllers;

[Route("api/stock-counts")]
public sealed class StockCountsController : ApiControllerBase
{
    private readonly IStockCountService _service;

    public StockCountsController(IStockCountService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockCountRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> UpsertLine(Guid id, [FromBody] CountLineRequest request, CancellationToken ct)
        => (await _service.UpsertLineAsync(id, request, ct)).ToActionResult();

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await _service.ApproveAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => (await _service.CancelAsync(id, ct)).ToActionResult();
}
