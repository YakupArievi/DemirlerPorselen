namespace Toptanci.Domain.Common;

/// <summary>
/// Tüm entity'ler için temel sınıf. PK = Guid (offline'da client tarafında üretilebilmesi için).
/// </summary>
public abstract class BaseEntity : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Denetim (audit) ve soft delete içeren temel entity. Sistemdeki entity'lerin çoğu bundan türer.
/// Not: Optimistic concurrency için SQL Server'a özel rowversion kullanılmaz (DB-bağımsızlık).
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
}
