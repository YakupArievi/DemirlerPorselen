using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Veritabanında saklanan refresh token. Rotation uygulanır: kullanıldığında iptal edilir
/// ve yerine geçen token'a referans verilir.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsActive(DateTime utcNow) => RevokedAt is null && !IsExpired(utcNow);
}
