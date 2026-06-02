using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>Kırık/hasarlı ürün kaydı. Kırık tipinde stok hareketi (çıkış) üretir.</summary>
public class BrokenProductRecord : AuditableEntity
{
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int Quantity { get; set; }
    public DateTime RecordDate { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
}
