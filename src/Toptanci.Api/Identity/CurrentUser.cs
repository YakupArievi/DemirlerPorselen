using System.Security.Claims;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Api.Identity;

/// <summary>
/// HttpContext üzerinden o anki kullanıcıyı çözer. JWT auth (Faz 0.3) eklenince
/// claim'ler dolacak; o zamana kadar IsAuthenticated=false döner.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);
}
