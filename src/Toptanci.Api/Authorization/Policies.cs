namespace Toptanci.Api.Authorization;

/// <summary>Yetki politikası adları. [Authorize(Policy = ...)] ile kullanılır.</summary>
public static class Policies
{
    /// <summary>Yalnızca Admin.</summary>
    public const string AdminOnly = nameof(AdminOnly);

    /// <summary>Patron veya Admin (ör. fiyat değişimi, raporlar).</summary>
    public const string PatronOrAdmin = nameof(PatronOrAdmin);

    /// <summary>Depo işlemleri: Depocu, Patron veya Admin.</summary>
    public const string WarehouseStaff = nameof(WarehouseStaff);

    /// <summary>Mobil portal müşterisi ("Portal" şeması + Customer rolü).</summary>
    public const string Portal = nameof(Portal);
}
