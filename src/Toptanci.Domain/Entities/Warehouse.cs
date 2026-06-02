using Toptanci.Domain.Common;

namespace Toptanci.Domain.Entities;

public class Warehouse : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
