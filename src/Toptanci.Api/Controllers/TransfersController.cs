using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Common;
using Toptanci.Application.Features.Operations.Transfers;

namespace Toptanci.Api.Controllers;

[Route("api/transfers")]
public sealed class TransfersController : ApiControllerBase
{
    private readonly IWarehouseTransferService _service;

    public TransfersController(IWarehouseTransferService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PageQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest request, CancellationToken ct)
        => (await _service.TransferAsync(request, ct)).ToActionResult();
}
