using Toptanci.Domain.Enums;

namespace Toptanci.Application.Common.Abstractions;

/// <summary>
/// Varyantlar için benzersiz sistem barkodu üretir (prefix + sıra numarası).
/// Adet ve koli için ayrı barkod.
/// </summary>
public interface IBarcodeGenerator
{
    Task<(string AdetBarcode, string KoliBarcode)> GenerateForVariantAsync(CancellationToken cancellationToken = default);
}

/// <summary>Bir barkodun çözümlenmiş hali: hangi varyant ve hangi birim tipi.</summary>
public sealed record ResolvedBarcode(Guid VariantId, UnitType UnitType);
