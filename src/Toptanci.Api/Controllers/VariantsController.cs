using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Catalog.Variants;

namespace Toptanci.Api.Controllers;

public sealed class VariantsController : ApiControllerBase
{
    private readonly IVariantService _service;
    private readonly IPriceService _priceService;

    public VariantsController(IVariantService service, IPriceService priceService)
    {
        _service = service;
        _priceService = priceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Toptanci.Application.Common.PageQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] string barcode, CancellationToken ct)
        => (await _service.ResolveBarcodeAsync(barcode, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVariantRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVariantRequest request, CancellationToken ct)
        => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();

    /// <summary>Fiyat değişikliği — yalnızca Patron/Admin. Fiyat geçmişine kaydedilir.</summary>
    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpPut("{id:guid}/price")]
    public async Task<IActionResult> ChangePrice(Guid id, [FromBody] ChangePriceRequest request, CancellationToken ct)
        => (await _priceService.ChangePriceAsync(id, request, ct)).ToActionResult();

    [HttpGet("{id:guid}/price-history")]
    public async Task<IActionResult> GetPriceHistory(Guid id, CancellationToken ct)
        => (await _priceService.GetHistoryAsync(id, ct)).ToActionResult();
}
