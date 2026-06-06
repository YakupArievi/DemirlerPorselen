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

    // Migration'lar provider başına ayrı assembly'lerde tutulur (aynı anda iki DB desteği)
    public const string SqlServerMigrationsAssembly = "Toptanci.Migrations.SqlServer";
    public const string PostgresMigrationsAssembly = "Toptanci.Migrations.Postgres";

    public static void Configure(DbContextOptionsBuilder options, string? provider, string? connectionString)
    {
        if (Normalize(provider) == Postgres)
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(PostgresMigrationsAssembly));
        }
        else
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
        }
    }
}
