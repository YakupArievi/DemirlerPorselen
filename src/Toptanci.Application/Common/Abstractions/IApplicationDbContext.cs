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

    DbSet<Customer> Customers { get; }
    DbSet<AccountTransaction> AccountTransactions { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleLine> SaleLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<SaleReturn> SaleReturns { get; }
    DbSet<SaleReturnLine> SaleReturnLines { get; }
    DbSet<PriceHistory> PriceHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
