using FluentValidation;
using Toptanci.Application.Common;

namespace Toptanci.Application.Features.Catalog.Products;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    bool IsActive,
    int VariantCount);

public sealed record CreateProductRequest(string Name, string? Description, Guid CategoryId, Guid? BrandId);

public sealed record UpdateProductRequest(string Name, string? Description, Guid CategoryId, Guid? BrandId, bool IsActive);

public sealed record ProductQuery : PageQuery
{
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
