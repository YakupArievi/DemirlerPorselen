using FluentValidation;
using Toptanci.Application.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales.Payments;

public sealed record CreatePaymentRequest(
    Guid CustomerId,
    Guid? SaleId,
    PaymentType Type,
    decimal Amount,
    DateTime? PaymentDate,
    DateTime? DueDate,
    string? IdempotencyKey,
    string? Note);

public sealed record PaymentDto(
    Guid Id, Guid CustomerId, string CustomerName, Guid? SaleId,
    PaymentType Type, decimal Amount, DateTime PaymentDate, DateTime? DueDate, string? Note);

public sealed record PaymentQuery : PageQuery
{
    public Guid? CustomerId { get; set; }
    public PaymentType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.DueDate).NotNull().When(x => x.Type == PaymentType.Cek)
            .WithMessage("Çek için vade tarihi zorunludur.");
    }
}
