using FluentValidation;
using Toptanci.Application.Common;

namespace Toptanci.Application.Features.Sales.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    string? TaxNumber,
    string? Notes,
    decimal OpeningBalance,
    decimal Balance,
    bool IsActive);

public sealed record CreateCustomerRequest(
    string Name, string? Phone, string? Address, string? TaxNumber, string? Notes, decimal OpeningBalance);

public sealed record UpdateCustomerRequest(
    string Name, string? Phone, string? Address, string? TaxNumber, string? Notes, decimal OpeningBalance, bool IsActive);

public sealed record CustomerQuery : PageQuery
{
    public bool? IsActive { get; set; }
    public bool? OnlyWithBalance { get; set; }
}

/// <summary>Mobil portal giriş bilgisi atama. Phone boşsa mevcut telefon kullanılır.</summary>
public sealed record SetPortalCredentialsRequest(string? Phone, string Password, bool Enabled);

public sealed class SetPortalCredentialsValidator : AbstractValidator<SetPortalCredentialsRequest>
{
    public SetPortalCredentialsValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(4).When(x => x.Enabled);
    }
}

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
