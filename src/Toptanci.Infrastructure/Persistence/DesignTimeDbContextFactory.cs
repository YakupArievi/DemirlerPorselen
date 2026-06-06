using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// EF Core tasarım-zamanı araçları (migrations add / database update) için context üretir.
/// Bu sayede 'dotnet ef' komutları API host'unu (ve başlangıç seed kodunu) çalıştırmaz.
///
/// Provider ve connection string ortam değişkenlerinden okunur (migration üretirken):
///   TOPTANCI_DB_PROVIDER  = SqlServer | Postgres   (varsayılan SqlServer)
///   TOPTANCI_DB_CONNECTION = bağlantı dizesi        (verilmezse provider'a göre yerel varsayılan)
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var provider = DbProviderConfigurator.Normalize(Environment.GetEnvironmentVariable("TOPTANCI_DB_PROVIDER"));
        var connectionString = Environment.GetEnvironmentVariable("TOPTANCI_DB_CONNECTION")
            ?? (provider == DbProviderConfigurator.Postgres
                ? "Host=localhost;Port=5432;Database=ToptanciDb;Username=postgres;Password=postgres"
                : "Server=(localdb)\\MSSQLLocalDB;Database=ToptanciDb;Trusted_Connection=True;TrustServerCertificate=True;");

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        DbProviderConfigurator.Configure(builder, provider, connectionString);
        return new ApplicationDbContext(builder.Options);
    }
}
