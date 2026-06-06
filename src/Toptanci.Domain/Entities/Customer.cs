using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Müşteri (cari). Bakiye = AçılışBakiyesi + hareketlerin işaretli toplamı (cache).
/// Pozitif bakiye = müşteri bize borçlu.
/// </summary>
public class Customer : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Notes { get; set; }

    /// <summary>Açılış (devir) bakiyesi. Pozitif = müşteri borçlu başlar.</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>Cache'lenmiş güncel bakiye (hareketlerden yeniden hesaplanabilir).</summary>
    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;

    // --- Mobil müşteri portalı (Faz 7) ---
    /// <summary>Portal girişi açık mı? Açıksa Phone + PasswordHash ile giriş yapılabilir.</summary>
    public bool PortalEnabled { get; set; }

    /// <summary>Portal parolası/PIN özeti (PBKDF2). Telefon kullanıcı adı olarak kullanılır.</summary>
    public string? PasswordHash { get; set; }

    public ICollection<AccountTransaction> AccountTransactions { get; set; } = new List<AccountTransaction>();
    public ICollection<CustomerRefreshToken> RefreshTokens { get; set; } = new List<CustomerRefreshToken>();
}
