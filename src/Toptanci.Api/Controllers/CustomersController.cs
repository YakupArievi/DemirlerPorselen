using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Sales.Accounts;
using Toptanci.Application.Features.Sales.Customers;

namespace Toptanci.Api.Controllers;

public sealed class CustomersController : ApiControllerBase
{
    private readonly ICustomerService _service;
    private readonly IAccountService _account;

    public CustomersController(ICustomerService service, IAccountService account)
    {
        _service = service;
        _account = account;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CustomerQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpGet("{id:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken ct)
        => (await _account.GetBalanceAsync(id, ct)).ToActionResult();

    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => (await _account.GetStatementAsync(id, from, to, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
        => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
