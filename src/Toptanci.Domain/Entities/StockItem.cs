using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Bir varyantın bir depodaki adet bazlı miktarı. Bu yalnızca bir CACHE'tir (mimari kural 6):
/// gerçek kaynak StockMovement ledger'ıdır; hareketle aynı transaction içinde güncellenir.
/// Eksi stoğa izin verilir.
/// </summary>
public class StockItem : AuditableEntity
{
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Adet bazlı miktar (negatif olabilir).</summary>
    public int Quantity { get; set; }
}
