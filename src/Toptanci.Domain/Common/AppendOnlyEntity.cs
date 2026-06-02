namespace Toptanci.Domain.Common;

/// <summary>
/// Değişmez (append-only) ledger kayıtları için temel sınıf: StockMovement, AccountTransaction, PriceHistory.
/// CreatedAt/By denetimi vardır ama soft delete YOKTUR (ledger kayıtları gizlenmemeli/silinmemeli).
/// </summary>
public abstract class AppendOnlyEntity : BaseEntity, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
