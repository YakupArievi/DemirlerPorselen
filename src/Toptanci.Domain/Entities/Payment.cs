using Toptanci.Domain.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Domain.Entities;

public class Payment : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Opsiyonel: belirli bir satışa karşılık.</summary>
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public PaymentType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    /// <summary>Çek için vade tarihi.</summary>
    public DateTime? DueDate { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }
}
