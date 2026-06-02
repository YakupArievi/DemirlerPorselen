using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Infrastructure.Security;

/// <summary>
/// Benzersiz barkod üretir: tek bir sıra numarası → adet "A"+12hane, koli "K"+12hane.
/// Farklı prefix'ler sayesinde adet/koli barkodları çakışmaz.
/// </summary>
public sealed class BarcodeGenerator : IBarcodeGenerator
{
    private readonly ISequenceGenerator _sequence;

    public BarcodeGenerator(ISequenceGenerator sequence) => _sequence = sequence;

    public async Task<(string AdetBarcode, string KoliBarcode)> GenerateForVariantAsync(
        CancellationToken cancellationToken = default)
    {
        var next = await _sequence.NextAsync(SequenceNames.Barcode, cancellationToken);
        var serial = next.ToString("D12");
        return ($"A{serial}", $"K{serial}");
    }
}
