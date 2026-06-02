using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

public class Brand : AuditableEntity
{
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
