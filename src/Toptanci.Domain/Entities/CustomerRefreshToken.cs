using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Mobil müşteri portalı için refresh token (personel RefreshToken'ından ayrı, izole).
/// Rotation uygulanır.
/// </summary>
public class CustomerRefreshToken : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Token { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsActive(DateTime utcNow) => RevokedAt is null && !IsExpired(utcNow);
}
