namespace Toptanci.Application.Common.Security;

/// <summary>
/// Rol adları (string). UserRole enum'unun ToString() değerleriyle birebir aynı olmalı;
/// JWT role claim'i ve [Authorize(Roles=...)] / policy'ler bu sabitleri kullanır.
/// </summary>
public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string Patron = nameof(Patron);
    public const string Depocu = nameof(Depocu);
}
