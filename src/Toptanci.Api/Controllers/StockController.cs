using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Common;
using Toptanci.Application.Features.Stock.ProductEntry;
using Toptanci.Application.Features.Stock.Queries;

namespace Toptanci.Api.Controllers;

public sealed class StockController : ApiControllerBase
{
    private readonly IStockQueryService _query;
    private readonly IProductEntryService _entry;

    public StockController(IStockQueryService query, IProductEntryService entry)
    {
        _query = query;
        _entry = entry;
    }

    [HttpGet("variant/{variantId:guid}")]
    public async Task<IActionResult> GetByVariant(Guid variantId, CancellationToken ct)
        => Ok(await _query.GetByVariantAsync(variantId, ct));

    [HttpGet("warehouse/{warehouseId:guid}")]
    public async Task<IActionResult> GetByWarehouse(Guid warehouseId, [FromQuery] PageQuery query, CancellationToken ct)
        => Ok(await _query.GetByWarehouseAsync(warehouseId, query, ct));

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] MovementQuery query, CancellationToken ct)
        => Ok(await _query.GetMovementsAsync(query, ct));

    [HttpPost("entry")]
    public async Task<IActionResult> Entry([FromBody] ProductEntryRequest request, CancellationToken ct)
        => (await _entry.ReceiveAsync(request, ct)).ToActionResult();
}
