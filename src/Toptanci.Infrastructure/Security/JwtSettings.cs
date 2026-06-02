namespace Toptanci.Infrastructure.Security;

/// <summary>appsettings'teki "Jwt" bölümünden bağlanan JWT ayarları.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Toptanci";
    public string Audience { get; set; } = "ToptanciClients";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
