using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Common;

namespace Toptanci.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges öncesinde audit alanlarını (CreatedAt/By, ModifiedAt/By) otomatik doldurur
/// ve fiziksel silme isteklerini soft delete'e çevirir.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
            return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = _currentUser.UserName ?? _currentUser.UserId?.ToString() ?? "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = user;
                        break;
                    case EntityState.Modified:
                        auditable.ModifiedAt = now;
                        auditable.ModifiedBy = user;
                        break;
                }
            }

            // Fiziksel silme -> soft delete
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete softDelete)
            {
                entry.State = EntityState.Modified;
                softDelete.IsDeleted = true;
                softDelete.DeletedAt = now;
                softDelete.DeletedBy = user;

                if (entry.Entity is IAuditableEntity a)
                {
                    a.ModifiedAt = now;
                    a.ModifiedBy = user;
                }
            }
        }
    }
}
