using Toptanci.Domain.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Domain.Entities;

public class Sale : AuditableEntity
{
    public long SaleNumber { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public DateTime SaleDate { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Active;

    /// <summary>Satır toplamları (iskontodan önce).</summary>
    public decimal SubTotal { get; set; }

    /// <summary>Satır iskontoları + fiş iskontosu toplamı.</summary>
    public decimal DiscountTotal { get; set; }

    /// <summary>Ödenecek net tutar (SubTotal - DiscountTotal).</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>Toplam maliyet (satır maliyet snapshot'larının toplamı).</summary>
    public decimal CostTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();
}

public class SaleLine : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public UnitType UnitType { get; set; }

    /// <summary>Seçilen birimde miktar (adet veya koli).</summary>
    public int Quantity { get; set; }

    /// <summary>Adete normalize miktar.</summary>
    public int AdetQuantity { get; set; }

    /// <summary>O anki satış fiyatından snapshot (seçilen birim başına).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>O anki ortalama maliyetten snapshot (adet başına).</summary>
    public decimal UnitCost { get; set; }

    public decimal LineDiscount { get; set; }

    /// <summary>Satır net tutarı: UnitPrice*Quantity - LineDiscount.</summary>
    public decimal LineTotal { get; set; }
}
