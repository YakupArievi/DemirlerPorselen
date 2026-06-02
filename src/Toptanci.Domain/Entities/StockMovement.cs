using Toptanci.Domain.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Değişmez (append-only) stok hareketi. Stok her zaman bu kayıtlardan türetilebilir (mimari kural 6).
/// </summary>
public class StockMovement : AppendOnlyEntity
{
    public StockMovementType Type { get; set; }

    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Adet bazlı, işaretli miktar (+ giriş, - çıkış).</summary>
    public int Quantity { get; set; }

    /// <summary>İşlem anındaki birim maliyet (snapshot, opsiyonel — örn. girişte alış fiyatı).</summary>
    public decimal? UnitCost { get; set; }

    /// <summary>Referans belge tipi (örn. "Sale", "ProductEntry", "StockCount").</summary>
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    /// <summary>Tekrarlı işlemleri (offline senkron) engellemek için benzersiz anahtar.</summary>
    public string? IdempotencyKey { get; set; }

    public string? Note { get; set; }
}
