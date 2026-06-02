using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toptanci.Domain.Entities;

namespace Toptanci.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.Property(w => w.Code).HasMaxLength(20);
        builder.HasIndex(w => w.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
    }
}

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");

        // Bir varyant bir depoda yalnızca bir kez bulunur
        builder.HasIndex(s => new { s.VariantId, s.WarehouseId }).IsUnique();

        builder.HasOne(s => s.Variant)
            .WithMany(v => v.StockItems)
            .HasForeignKey(s => s.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.StockItems)
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.Property(m => m.UnitCost).HasPrecision(18, 4);
        builder.Property(m => m.ReferenceType).HasMaxLength(50);
        builder.Property(m => m.IdempotencyKey).HasMaxLength(100);
        builder.Property(m => m.Note).HasMaxLength(500);

        builder.HasIndex(m => m.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(m => new { m.VariantId, m.WarehouseId });
        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId });

        builder.HasOne(m => m.Variant)
            .WithMany()
            .HasForeignKey(m => m.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Warehouse)
            .WithMany()
            .HasForeignKey(m => m.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
