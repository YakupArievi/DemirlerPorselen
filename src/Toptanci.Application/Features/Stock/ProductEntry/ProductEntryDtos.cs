using FluentValidation;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Stock.ProductEntry;

public sealed record ProductEntryLine(
    Guid VariantId,
    UnitType UnitType,
    int Quantity,
    decimal UnitPurchasePrice);

public sealed record ProductEntryRequest(
    Guid WarehouseId,
    IReadOnlyList<ProductEntryLine> Lines,
    string? IdempotencyKey,
    string? Note);

public sealed record ProductEntryResultDto(int LineCount, int TotalAdet, bool AlreadyProcessed);

public sealed class ProductEntryValidator : AbstractValidator<ProductEntryRequest>
{
    public ProductEntryValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.VariantId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPurchasePrice).GreaterThanOrEqualTo(0);
        });
    }
}
