namespace Toptanci.Domain.Common;

/// <summary>Tüm entity'lerin Guid kimliği.</summary>
public interface IEntity
{
    Guid Id { get; }
}

/// <summary>Oluşturma/değiştirme denetim alanları.</summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string? ModifiedBy { get; set; }
}

/// <summary>Fiziksel silme yerine işaretleme.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
