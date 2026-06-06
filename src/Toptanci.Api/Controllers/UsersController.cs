using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Admin.Users;

namespace Toptanci.Api.Controllers;

/// <summary>Personel (kullanıcı) yönetimi — yalnızca Admin.</summary>
[Authorize(Policy = Policies.AdminOnly)]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
        => (await _service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
        => (await _service.ResetPasswordAsync(id, request, ct)).ToActionResult();
}
