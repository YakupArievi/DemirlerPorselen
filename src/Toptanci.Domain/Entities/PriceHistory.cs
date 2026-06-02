using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

/// <summary>
/// Varyant fiyat değişimlerinin tarihçesi (denetim/raporlama).
/// Eski satışlar snapshot sayesinde değişmez; bu tablo yalnızca izleme amaçlıdır.
/// </summary>
public class PriceHistory : AppendOnlyEntity
{
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public decimal OldPurchasePrice { get; set; }
    public decimal NewPurchasePrice { get; set; }
    public decimal OldSalePrice { get; set; }
    public decimal NewSalePrice { get; set; }
}
