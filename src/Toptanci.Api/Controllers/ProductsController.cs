using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Catalog.Products;
using Toptanci.Application.Features.Catalog.Variants;

namespace Toptanci.Api.Controllers;

public sealed class ProductsController : ApiControllerBase
{
    private readonly IProductService _service;
    private readonly IVariantService _variantService;

    public ProductsController(IProductService service, IVariantService variantService)
    {
        _service = service;
        _variantService = variantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpGet("{id:guid}/variants")]
    public async Task<IActionResult> GetVariants(Guid id, CancellationToken ct)
        => (await _variantService.GetByProductAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
