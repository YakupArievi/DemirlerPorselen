using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Ürün varyantı. Stok, fiyat ve barkod VARYANT seviyesinde tutulur (mimari kural 12).
/// </summary>
public class ProductVariant : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string? Color { get; set; }
    public string? Pattern { get; set; }
    public string? Size { get; set; }

    /// <summary>Adet (tekil) barkodu — sistem üretir, benzersiz.</summary>
    public string AdetBarcode { get; set; } = null!;

    /// <summary>Koli barkodu — sistem üretir, benzersiz.</summary>
    public string KoliBarcode { get; set; } = null!;

    /// <summary>Bir kolinin içindeki adet sayısı. Koli hareketleri adete çevrilirken kullanılır.</summary>
    public int KoliIciAdet { get; set; } = 1;

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }

    /// <summary>Ağırlıklı ortalama alış maliyeti (mimari kural 10). Ürün girişinde güncellenir.</summary>
    public decimal AverageCost { get; set; }

    public int MinStock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
