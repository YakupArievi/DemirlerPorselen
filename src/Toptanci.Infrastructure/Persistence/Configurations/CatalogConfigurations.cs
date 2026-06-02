using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toptanci.Domain.Entities;

namespace Toptanci.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.HasIndex(c => c.Name);
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(b => b.Name);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.HasIndex(p => p.Name);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.Pattern).HasMaxLength(50);
        builder.Property(v => v.Size).HasMaxLength(50);
        builder.Property(v => v.AdetBarcode).IsRequired().HasMaxLength(64);
        builder.Property(v => v.KoliBarcode).IsRequired().HasMaxLength(64);
        builder.Property(v => v.ImageUrl).HasMaxLength(500);

        builder.Property(v => v.PurchasePrice).HasPrecision(18, 2);
        builder.Property(v => v.SalePrice).HasPrecision(18, 2);
        builder.Property(v => v.AverageCost).HasPrecision(18, 4);

        builder.HasIndex(v => v.AdetBarcode).IsUnique();
        builder.HasIndex(v => v.KoliBarcode).IsUnique();
        builder.HasIndex(v => v.ProductId);

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
