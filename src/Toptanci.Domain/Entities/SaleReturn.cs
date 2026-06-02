using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Satış iadesi (kısmi olabilir). Orijinal satış silinmez; iade ayrı kayıt olarak saklanır (audit).
/// </summary>
public class SaleReturn : AuditableEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }

    public DateTime ReturnDate { get; set; }

    /// <summary>İade edilen net tutar (cariye alacak olarak işlenir).</summary>
    public decimal TotalAmount { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }

    public ICollection<SaleReturnLine> Lines { get; set; } = new List<SaleReturnLine>();
}

public class SaleReturnLine : BaseEntity
{
    public Guid SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = null!;

    public Guid SaleLineId { get; set; }
    public Guid VariantId { get; set; }

    /// <summary>İade edilen adet (adete normalize).</summary>
    public int AdetQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
