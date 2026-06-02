using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);

        // Kullanıcı adı veya parola hatalı -> aynı mesaj (kullanıcı sızıntısını önle)
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(Error.Unauthorized("Kullanıcı adı veya parola hatalı."));

        if (!user.IsActive)
            return Result.Failure<AuthResponse>(Error.Unauthorized("Hesap pasif durumda."));

        var response = await IssueTokensAsync(user, cancellationToken);
        return Result.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (existing is null || !existing.IsActive(now))
            return Result.Failure<AuthResponse>(Error.Unauthorized("Geçersiz veya süresi dolmuş oturum."));

        if (!existing.User.IsActive)
            return Result.Failure<AuthResponse>(Error.Unauthorized("Hesap pasif durumda."));

        // Rotation: eski token'ı iptal et, yenisini üret
        var newRefresh = _tokenService.GenerateRefreshToken();
        existing.RevokedAt = now;
        existing.ReplacedByToken = newRefresh.Token;

        var response = await IssueTokensAsync(existing.User, cancellationToken, newRefresh);
        return Result.Success(response);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        // İdempotent: token yoksa veya zaten iptalse de başarı dön
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, CancellationToken cancellationToken, TokenResult? refreshToken = null)
    {
        var access = _tokenService.GenerateAccessToken(user);
        var refresh = refreshToken ?? _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh.Token,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = refresh.ExpiresAt
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            access.Token,
            access.ExpiresAt,
            refresh.Token,
            refresh.ExpiresAt,
            new UserInfo(user.Id, user.UserName, user.FullName, user.Role));
    }
}
