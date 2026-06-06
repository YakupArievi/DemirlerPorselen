using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Portal;

public sealed record PortalLoginRequest(string Phone, string Password);
public sealed record PortalRefreshRequest(string RefreshToken);
public sealed record PortalCustomerInfo(Guid Id, string Name, string? Phone, decimal Balance);
public sealed record PortalAuthResponse(
    string AccessToken, DateTime AccessTokenExpiresAt,
    string RefreshToken, DateTime RefreshTokenExpiresAt,
    PortalCustomerInfo Customer);

public sealed class PortalLoginValidator : AbstractValidator<PortalLoginRequest>
{
    public PortalLoginValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed record ChangePortalPasswordRequest(string CurrentPassword, string NewPassword);

public sealed class ChangePortalPasswordValidator : AbstractValidator<ChangePortalPasswordRequest>
{
    public ChangePortalPasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(4);
    }
}

public interface IPortalAuthService
{
    Task<Result<PortalAuthResponse>> LoginAsync(PortalLoginRequest request, CancellationToken ct = default);
    Task<Result<PortalAuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid customerId, ChangePortalPasswordRequest request, CancellationToken ct = default);
}

public sealed class PortalAuthService : IPortalAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public PortalAuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService, TimeProvider timeProvider)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PortalAuthResponse>> LoginAsync(PortalLoginRequest request, CancellationToken ct = default)
    {
        var phone = request.Phone.Trim();
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.PortalEnabled && c.Phone == phone, ct);

        if (customer is null || customer.PasswordHash is null || !_passwordHasher.Verify(request.Password, customer.PasswordHash))
            return Result.Failure<PortalAuthResponse>(Error.Unauthorized("Telefon veya parola hatalı."));
        if (!customer.IsActive)
            return Result.Failure<PortalAuthResponse>(Error.Unauthorized("Hesap pasif durumda."));

        return Result.Success(await IssueAsync(customer, ct));
    }

    public async Task<Result<PortalAuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var existing = await _db.CustomerRefreshTokens
            .Include(rt => rt.Customer)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct);

        if (existing is null || !existing.IsActive(now) || !existing.Customer.PortalEnabled || !existing.Customer.IsActive)
            return Result.Failure<PortalAuthResponse>(Error.Unauthorized("Geçersiz veya süresi dolmuş oturum."));

        var newRefresh = _tokenService.GenerateRefreshToken();
        existing.RevokedAt = now;
        existing.ReplacedByToken = newRefresh.Token;

        return Result.Success(await IssueAsync(existing.Customer, ct, newRefresh));
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var existing = await _db.CustomerRefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = now;
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid customerId, ChangePortalPasswordRequest request, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.PortalEnabled, ct);
        if (customer is null || customer.PasswordHash is null)
            return Result.Failure(Error.NotFound("Hesap bulunamadı."));
        if (!_passwordHasher.Verify(request.CurrentPassword, customer.PasswordHash))
            return Result.Failure(Error.Unauthorized("Mevcut parola hatalı."));

        customer.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<PortalAuthResponse> IssueAsync(Customer customer, CancellationToken ct, TokenResult? refresh = null)
    {
        var access = _tokenService.GenerateCustomerAccessToken(customer);
        var rt = refresh ?? _tokenService.GenerateRefreshToken();

        _db.CustomerRefreshTokens.Add(new CustomerRefreshToken
        {
            CustomerId = customer.Id,
            Token = rt.Token,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = rt.ExpiresAt
        });
        await _db.SaveChangesAsync(ct);

        return new PortalAuthResponse(
            access.Token, access.ExpiresAt, rt.Token, rt.ExpiresAt,
            new PortalCustomerInfo(customer.Id, customer.Name, customer.Phone, customer.Balance));
    }
}
