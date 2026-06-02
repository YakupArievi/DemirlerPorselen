using FluentValidation;
using Toptanci.Application.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales.Sales;

public sealed record CreateSaleLine(
    Guid VariantId,
    UnitType UnitType,
    int Quantity,
    decimal? UnitPrice,
    decimal LineDiscount);

public sealed record InitialPayment(PaymentType Type, decimal Amount, DateTime? DueDate);

public sealed record CreateSaleRequest(
    Guid CustomerId,
    Guid WarehouseId,
    DateTime? SaleDate,
    IReadOnlyList<CreateSaleLine> Lines,
    decimal DocumentDiscount,
    InitialPayment? InitialPayment,
    string? IdempotencyKey,
    string? Note);

public sealed record SaleLineDto(
    Guid Id, Guid VariantId, string ProductName, string? Color, string? Size,
    UnitType UnitType, int Quantity, int AdetQuantity,
    decimal UnitPrice, decimal UnitCost, decimal LineDiscount, decimal LineTotal);

public sealed record SaleDto(
    Guid Id, long SaleNumber, Guid CustomerId, string CustomerName, Guid WarehouseId,
    DateTime SaleDate, SaleStatus Status,
    decimal SubTotal, decimal DiscountTotal, decimal GrandTotal, decimal CostTotal,
    decimal PaidAmount, decimal Profit, string? Note, DateTime? CancelledAt,
    IReadOnlyList<SaleLineDto> Lines);

public sealed record SaleListItemDto(
    Guid Id, long SaleNumber, string CustomerName, DateTime SaleDate,
    SaleStatus Status, decimal GrandTotal, decimal PaidAmount);

public sealed record SaleQuery : PageQuery
{
    public Guid? CustomerId { get; set; }
    public SaleStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public sealed record ReturnLine(Guid SaleLineId, int AdetQuantity);

public sealed record ReturnSaleRequest(
    Guid SaleId, IReadOnlyList<ReturnLine> Lines, string? IdempotencyKey, string? Note);

public sealed class CreateSaleValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.DocumentDiscount).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.VariantId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
            l.RuleFor(x => x.LineDiscount).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).When(x => x.UnitPrice.HasValue);
        });
    }
}

public sealed class ReturnSaleValidator : AbstractValidator<ReturnSaleRequest>
{
    public ReturnSaleValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.SaleLineId).NotEmpty();
            l.RuleFor(x => x.AdetQuantity).GreaterThan(0);
        });
    }
}
