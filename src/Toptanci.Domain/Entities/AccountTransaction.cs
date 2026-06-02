using Toptanci.Domain.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Değişmez (append-only) cari hesap hareketi (mimari kural 7).
/// Bakiye bu hareketlerden türetilebilir.
/// </summary>
public class AccountTransaction : AppendOnlyEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public AccountTransactionType Type { get; set; }
    public AccountDirection Direction { get; set; }

    /// <summary>Pozitif tutar. Bakiyeye etkisi Direction ile belirlenir.</summary>
    public decimal Amount { get; set; }

    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }

    /// <summary>Bakiyeye işaretli etki: Borç (+), Alacak (-).</summary>
    public decimal SignedAmount => Direction == AccountDirection.Borc ? Amount : -Amount;
}
