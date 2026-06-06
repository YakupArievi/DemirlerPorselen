using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toptanci.Api.Authorization;
using Toptanci.Api.Extensions;
using Toptanci.Application.Features.Portal;

namespace Toptanci.Api.Controllers;

[ApiController]
[Route("api/portal/auth")]
public sealed class PortalAuthController : ControllerBase
{
    private readonly IPortalAuthService _service;

    public PortalAuthController(IPortalAuthService service) => _service = service;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] PortalLoginRequest request, CancellationToken ct)
        => (await _service.LoginAsync(request, ct)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] PortalRefreshRequest request, CancellationToken ct)
        => (await _service.RefreshAsync(request.RefreshToken, ct)).ToActionResult();

    [Authorize(Policy = Policies.Portal)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] PortalRefreshRequest request, CancellationToken ct)
        => (await _service.LogoutAsync(request.RefreshToken, ct)).ToActionResult();
}
