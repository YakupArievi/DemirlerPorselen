using Toptanci.Domain.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Domain.Entities;

/// <summary>Stok sayım fişi (depo bazlı). Onaylandığında SayımFarkı hareketleri üretir.</summary>
public class StockCount : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public DateTime CountDate { get; set; }
    public StockCountStatus Status { get; set; } = StockCountStatus.Draft;
    public DateTime? ApprovedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}

public class StockCountLine : BaseEntity
{
    public Guid StockCountId { get; set; }
    public StockCount StockCount { get; set; } = null!;

    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    /// <summary>Sayılan (fiziksel) adet.</summary>
    public int CountedQuantity { get; set; }

    /// <summary>Onay anında hesaplanan fark (sayılan - sistem). Onaydan önce 0.</summary>
    public int Difference { get; set; }
}
