using Toptanci.Domain.Entities;

namespace Toptanci.Application.Common.Abstractions;

/// <summary>JWT access token ve refresh token üretimi.</summary>
public interface ITokenService
{
    TokenResult GenerateAccessToken(User user);
    TokenResult GenerateRefreshToken();

    /// <summary>Mobil portal müşterisi için access token (rol=Customer, ayrı audience).</summary>
    TokenResult GenerateCustomerAccessToken(Customer customer);
}

/// <summary>Üretilen token ve son geçerlilik tarihi (UTC).</summary>
public sealed record TokenResult(string Token, DateTime ExpiresAt);
