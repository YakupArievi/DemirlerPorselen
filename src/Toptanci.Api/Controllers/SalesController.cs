using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Sales.Sales;

namespace Toptanci.Api.Controllers;

public sealed class SalesController : ApiControllerBase
{
    private readonly ISaleService _service;

    public SalesController(ISaleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SaleQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => (await _service.CancelAsync(id, ct)).ToActionResult();

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] ReturnSaleRequest request, CancellationToken ct)
        => (await _service.ReturnAsync(request, ct)).ToActionResult();
}
