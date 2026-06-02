using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Operations.Broken;

namespace Toptanci.Api.Controllers;

[Route("api/broken")]
public sealed class BrokenProductsController : ApiControllerBase
{
    private readonly IBrokenProductService _service;

    public BrokenProductsController(IBrokenProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] BrokenQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrokenProductRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();
}
