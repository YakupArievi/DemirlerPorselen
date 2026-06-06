using Microsoft.EntityFrameworkCore;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// DbContext'i seçilen veritabanı sağlayıcısına göre yapılandırır (DB-bağımsızlık).
/// Hem runtime (AddInfrastructure) hem design-time (migrations) aynı mantığı kullanır.
/// Provider: "SqlServer" (varsayılan) veya "Postgres".
/// </summary>
public static class DbProviderConfigurator
{
    public const string SqlServer = "SqlServer";
    public const string Postgres = "Postgres";

    public static string Normalize(string? provider)
        => string.Equals(provider, Postgres, StringComparison.OrdinalIgnoreCase) ? Postgres : SqlServer;

    public static void Configure(DbContextOptionsBuilder options, string? provider, string? connectionString)
    {
        var migrationsAssembly = typeof(ApplicationDbContext).Assembly.FullName;

        if (Normalize(provider) == Postgres)
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(migrationsAssembly));
        }
        else
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
        }
    }
}
