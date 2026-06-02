using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Toptanci.Domain.Common;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın ana EF Core context'i. Entity'ler ve konfigürasyonları sonraki fazlarda eklenecek.
/// Soft delete için global query filter ve assembly'deki IEntityTypeConfiguration'ları otomatik uygular.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bu assembly'deki tüm IEntityTypeConfiguration<T> sınıflarını uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ISoftDelete uygulayan tüm entity'lere global query filter ekle (e => !e.IsDeleted)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(prop), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }
}
