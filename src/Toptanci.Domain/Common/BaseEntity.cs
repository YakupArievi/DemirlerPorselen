using System.ComponentModel.DataAnnotations;

namespace Toptanci.Domain.Common;

/// <summary>
/// Tüm entity'ler için temel sınıf. PK = Guid (offline'da client tarafında üretilebilmesi için).
/// </summary>
public abstract class BaseEntity : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Denetim (audit), soft delete ve optimistic concurrency içeren temel entity.
/// Sistemdeki entity'lerin çoğu bundan türer.
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDelete
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    /// <summary>Optimistic concurrency için SQL Server rowversion (otomatik).</summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
