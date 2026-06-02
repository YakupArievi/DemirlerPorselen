using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>İki depo arası transfer fişi. Her satır için kaynaktan çıkış + hedefe giriş hareketi.</summary>
public class WarehouseTransfer : AuditableEntity
{
    public Guid SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; } = null!;

    public Guid TargetWarehouseId { get; set; }
    public Warehouse TargetWarehouse { get; set; } = null!;

    public DateTime TransferDate { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }

    public ICollection<WarehouseTransferLine> Lines { get; set; } = new List<WarehouseTransferLine>();
}

public class WarehouseTransferLine : BaseEntity
{
    public Guid WarehouseTransferId { get; set; }
    public WarehouseTransfer WarehouseTransfer { get; set; } = null!;

    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    /// <summary>Transfer edilen adet.</summary>
    public int Quantity { get; set; }
}
