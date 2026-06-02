using FluentValidation;

namespace Toptanci.Application.Features.Catalog.Brands;

public sealed record BrandDto(Guid Id, string Name, bool IsActive);

public sealed record CreateBrandRequest(string Name);

public sealed record UpdateBrandRequest(string Name, bool IsActive);

public sealed class CreateBrandValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class UpdateBrandValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}
