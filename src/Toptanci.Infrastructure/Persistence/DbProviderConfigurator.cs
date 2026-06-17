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
        connectionString = SanitizeConnectionString(connectionString);
        if (Normalize(provider) == Postgres)
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(PostgresMigrationsAssembly));
        }
        else
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
        }
    }

    /// <summary>
    /// Bulut panellerinde (Render/Vercel) VALUE kutusuna yanlışlıkla "Ad=deger" şeklinde,
    /// değişken adı önekiyle yapıştırma sık görülür. Bağlantı dizesi başına böyle bir önek
    /// yapışmışsa temizle ki kurulum hatası deploy'u kırmasın.
    /// </summary>
    private static string? SanitizeConnectionString(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return cs;
        cs = cs.Trim();
        const string accidentalPrefix = "ConnectionStrings__DefaultConnection=";
        if (cs.StartsWith(accidentalPrefix, StringComparison.OrdinalIgnoreCase))
            cs = cs[accidentalPrefix.Length..].Trim();
        return cs;
    }
}
