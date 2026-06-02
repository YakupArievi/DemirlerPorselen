using Microsoft.EntityFrameworkCore;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Common.Abstractions;

/// <summary>
/// Application katmanının veritabanına eriştiği soyutlama. DbSet'ler entity geldikçe eklenir.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }

    DbSet<Warehouse> Warehouses { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<StockMovement> StockMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
