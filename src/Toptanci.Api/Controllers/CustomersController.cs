using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
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

    /// <summary>Satış ekranı için müşteri arama — yalnızca id+ad (parasal bilgi yok). Tüm personel (Depocu dahil).</summary>
    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string? search, CancellationToken ct)
        => Ok(await _service.GetLookupAsync(search, ct));

    // --- Aşağısı cari/parasal bilgi: yalnızca Patron/Admin (Depocu göremez) ---

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CustomerQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpGet("{id:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken ct)
        => (await _account.GetBalanceAsync(id, ct)).ToActionResult();

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => (await _account.GetStatementAsync(id, from, to, ct)).ToActionResult();

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
        => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();

    /// <summary>Müşteriye mobil portal giriş bilgisi atar (yalnızca Patron/Admin).</summary>
    [Authorize(Policy = Policies.PatronOrAdmin)]
    [HttpPost("{id:guid}/portal")]
    public async Task<IActionResult> SetPortal(Guid id, [FromBody] SetPortalCredentialsRequest request, CancellationToken ct)
        => (await _service.SetPortalCredentialsAsync(id, request, ct)).ToActionResult();
}
