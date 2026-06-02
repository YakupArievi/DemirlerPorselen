using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Sales.Payments;

namespace Toptanci.Api.Controllers;

public sealed class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaymentQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();
}
