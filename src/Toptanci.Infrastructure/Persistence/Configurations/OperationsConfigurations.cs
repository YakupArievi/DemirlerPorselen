using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toptanci.Domain.Entities;

namespace Toptanci.Infrastructure.Persistence.Configurations;

public sealed class BrokenProductRecordConfiguration : IEntityTypeConfiguration<BrokenProductRecord>
{
    public void Configure(EntityTypeBuilder<BrokenProductRecord> builder)
    {
        builder.ToTable("BrokenProductRecords");
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.PhotoUrl).HasMaxLength(500);
        builder.HasIndex(b => b.VariantId);

        builder.HasOne(b => b.Variant).WithMany().HasForeignKey(b => b.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Warehouse).WithMany().HasForeignKey(b => b.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts");
        builder.Property(c => c.Note).HasMaxLength(500);
        builder.HasIndex(c => c.WarehouseId);

        builder.HasOne(c => c.Warehouse).WithMany().HasForeignKey(c => c.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Lines).WithOne(l => l.StockCount).HasForeignKey(l => l.StockCountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("StockCountLines");
        builder.HasIndex(l => new { l.StockCountId, l.VariantId }).IsUnique();
        builder.HasOne(l => l.Variant).WithMany().HasForeignKey(l => l.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WarehouseTransferConfiguration : IEntityTypeConfiguration<WarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<WarehouseTransfer> builder)
    {
        builder.ToTable("WarehouseTransfers");
        builder.Property(t => t.Note).HasMaxLength(500);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(100);
        builder.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasOne(t => t.SourceWarehouse).WithMany().HasForeignKey(t => t.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TargetWarehouse).WithMany().HasForeignKey(t => t.TargetWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(t => t.Lines).WithOne(l => l.WarehouseTransfer).HasForeignKey(l => l.WarehouseTransferId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WarehouseTransferLineConfiguration : IEntityTypeConfiguration<WarehouseTransferLine>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferLine> builder)
    {
        builder.ToTable("WarehouseTransferLines");
        builder.HasOne(l => l.Variant).WithMany().HasForeignKey(l => l.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
