namespace Toptanci.Application.Common.Abstractions;

/// <summary>
/// O anki istek için kimliği doğrulanmış kullanıcıyı temsil eder.
/// Audit alanlarını (CreatedBy/ModifiedBy) doldurmak için kullanılır.
/// Gerçek implementasyon Faz 0.3 (JWT auth) ile tamamlanır.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
