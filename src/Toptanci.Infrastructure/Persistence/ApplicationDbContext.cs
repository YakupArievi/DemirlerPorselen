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

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnLine> SaleReturnLines => Set<SaleReturnLine>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    public DbSet<BrokenProductRecord> BrokenProductRecords => Set<BrokenProductRecord>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<WarehouseTransfer> WarehouseTransfers => Set<WarehouseTransfer>();
    public DbSet<WarehouseTransferLine> WarehouseTransferLines => Set<WarehouseTransferLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bu assembly'deki tüm IEntityTypeConfiguration<T> sınıflarını uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Barkod ve satış numarası için monoton sıralar
        modelBuilder.HasSequence<long>(SequenceNames.Barcode).StartsAt(1).IncrementsBy(1);
        modelBuilder.HasSequence<long>(SequenceNames.SaleNumber).StartsAt(1000).IncrementsBy(1);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // ISoftDelete uygulayan tüm entity'lere global query filter ekle (e => !e.IsDeleted)
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(prop), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }

            // Guid PK'ler client tarafında üretilir (offline, mimari kural 1) -> ValueGeneratedNever.
            // Aksi halde mevcut parent'a önceden Id atanmış child eklenince EF "var olan kayıt" sanıp UPDATE üretir.
            var pk = entityType.FindPrimaryKey();
            if (pk is { Properties.Count: 1 } && pk.Properties[0] is { Name: "Id", ClrType: var t } idProp && t == typeof(Guid))
                idProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }
    }
}
