using FluentValidation;

namespace Toptanci.Application.Features.Stock.Warehouses;

public sealed record WarehouseDto(Guid Id, string Name, string? Code, bool IsDefault, bool IsActive);

public sealed record CreateWarehouseRequest(string Name, string? Code, bool IsDefault);

public sealed record UpdateWarehouseRequest(string Name, string? Code, bool IsDefault, bool IsActive);

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).MaximumLength(20);
    }
}

public sealed class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).MaximumLength(20);
    }
}
