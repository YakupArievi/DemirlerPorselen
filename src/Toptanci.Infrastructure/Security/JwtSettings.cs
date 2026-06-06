namespace Toptanci.Infrastructure.Security;

/// <summary>appsettings'teki "Jwt" bölümünden bağlanan JWT ayarları.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Toptanci";
    public string Audience { get; set; } = "ToptanciClients";
    /// <summary>Mobil müşteri portalı için ayrı audience (personel token'larından izolasyon).</summary>
    public string PortalAudience { get; set; } = "ToptanciPortal";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    /// <summary>Portal access token süresi (müşteri uygulaması için daha uzun).</summary>
    public int PortalAccessTokenMinutes { get; set; } = 240;
}
