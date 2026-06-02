using FluentValidation;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Catalog.Variants;

public sealed record VariantDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? Color,
    string? Pattern,
    string? Size,
    string AdetBarcode,
    string KoliBarcode,
    int KoliIciAdet,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal AverageCost,
    int MinStock,
    string? ImageUrl,
    bool IsActive);

public sealed record CreateVariantRequest(
    Guid ProductId,
    string? Color,
    string? Pattern,
    string? Size,
    int KoliIciAdet,
    decimal PurchasePrice,
    decimal SalePrice,
    int MinStock,
    string? ImageUrl);

public sealed record UpdateVariantRequest(
    string? Color,
    string? Pattern,
    string? Size,
    int KoliIciAdet,
    int MinStock,
    string? ImageUrl,
    bool IsActive);

/// <summary>Barkod çözümleme sonucu: hangi varyant, hangi birim, kaç adete denk.</summary>
public sealed record ResolvedBarcodeDto(
    Guid VariantId,
    string ProductName,
    string? Color,
    string? Pattern,
    string? Size,
    UnitType UnitType,
    int AdetEquivalent,
    decimal SalePrice);

public sealed class CreateVariantValidator : AbstractValidator<CreateVariantRequest>
{
    public CreateVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.KoliIciAdet).GreaterThan(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Pattern).MaximumLength(50);
        RuleFor(x => x.Size).MaximumLength(50);
    }
}

public sealed class UpdateVariantValidator : AbstractValidator<UpdateVariantRequest>
{
    public UpdateVariantValidator()
    {
        RuleFor(x => x.KoliIciAdet).GreaterThan(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Fiyat değişikliği (yalnızca Patron/Admin). Fiyat geçmişine kaydedilir.</summary>
public sealed record ChangePriceRequest(decimal PurchasePrice, decimal SalePrice);

public sealed record PriceHistoryDto(
    Guid Id, Guid VariantId,
    decimal OldPurchasePrice, decimal NewPurchasePrice,
    decimal OldSalePrice, decimal NewSalePrice,
    DateTime ChangedAt, string? ChangedBy);

public sealed class ChangePriceValidator : AbstractValidator<ChangePriceRequest>
{
    public ChangePriceValidator()
    {
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
    }
}
