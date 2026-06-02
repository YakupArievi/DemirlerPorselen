using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Auth;

public sealed record LoginRequest(string UserName, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserInfo User);

public sealed record UserInfo(Guid Id, string UserName, string FullName, UserRole Role);
