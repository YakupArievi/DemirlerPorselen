using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Common;
using Toptanci.Domain.Entities;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın ana EF Core context'i. Yeni entity'lerin DbSet'leri buraya eklenir.
/// Soft delete için global query filter ve assembly'deki IEntityTypeConfiguration'ları otomatik uygular.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public const string BarcodeSequenceName = "BarcodeSequence";

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bu assembly'deki tüm IEntityTypeConfiguration<T> sınıflarını uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Barkod üretimi için monoton sıra
        modelBuilder.HasSequence<long>(BarcodeSequenceName).StartsAt(1).IncrementsBy(1);

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
