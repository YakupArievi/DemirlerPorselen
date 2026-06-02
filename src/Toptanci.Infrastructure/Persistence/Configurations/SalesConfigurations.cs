using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toptanci.Domain.Entities;

namespace Toptanci.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.TaxNumber).HasMaxLength(20);
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.OpeningBalance).HasPrecision(18, 2);
        builder.Property(c => c.Balance).HasPrecision(18, 2);
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Phone);
    }
}

public sealed class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("AccountTransactions");
        builder.Ignore(t => t.SignedAmount);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.ReferenceType).HasMaxLength(50);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(100);
        builder.Property(t => t.Note).HasMaxLength(500);

        builder.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => new { t.ReferenceType, t.ReferenceId });

        builder.HasOne(t => t.Customer)
            .WithMany(c => c.AccountTransactions)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.Property(s => s.SubTotal).HasPrecision(18, 2);
        builder.Property(s => s.DiscountTotal).HasPrecision(18, 2);
        builder.Property(s => s.GrandTotal).HasPrecision(18, 2);
        builder.Property(s => s.CostTotal).HasPrecision(18, 4);
        builder.Property(s => s.PaidAmount).HasPrecision(18, 2);
        builder.Property(s => s.IdempotencyKey).HasMaxLength(100);
        builder.Property(s => s.Note).HasMaxLength(500);

        builder.HasIndex(s => s.SaleNumber).IsUnique();
        builder.HasIndex(s => s.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(s => s.CustomerId);

        builder.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines).WithOne(l => l.Sale).HasForeignKey(l => l.SaleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines");
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineDiscount).HasPrecision(18, 2);
        builder.Property(l => l.LineTotal).HasPrecision(18, 2);

        builder.HasOne(l => l.Variant).WithMany().HasForeignKey(l => l.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(100);
        builder.Property(p => p.Note).HasMaxLength(500);

        builder.HasIndex(p => p.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(p => p.CustomerId);

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Sale).WithMany().HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("SaleReturns");
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.Property(r => r.IdempotencyKey).HasMaxLength(100);
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasIndex(r => r.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(r => r.SaleId);

        builder.HasOne(r => r.Sale).WithMany().HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines).WithOne(l => l.SaleReturn).HasForeignKey(l => l.SaleReturnId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SaleReturnLineConfiguration : IEntityTypeConfiguration<SaleReturnLine>
{
    public void Configure(EntityTypeBuilder<SaleReturnLine> builder)
    {
        builder.ToTable("SaleReturnLines");
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 2);
    }
}

public sealed class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories");
        builder.Property(p => p.OldPurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.NewPurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.OldSalePrice).HasPrecision(18, 2);
        builder.Property(p => p.NewSalePrice).HasPrecision(18, 2);
        builder.HasIndex(p => p.VariantId);

        builder.HasOne(p => p.Variant).WithMany().HasForeignKey(p => p.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
