using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

public class Product : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
