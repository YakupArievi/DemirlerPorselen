using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

public class Category : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
