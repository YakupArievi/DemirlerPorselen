namespace Toptanci.Domain.Enums;

/// <summary>Sistem rolleri. Her kullanıcının tek bir rolü vardır.</summary>
public enum UserRole
{
    /// <summary>Tam yetki: kullanıcı yönetimi dahil her şey.</summary>
    Admin = 1,

    /// <summary>İşletme sahibi: fiyat değişimi, raporlar, satış/cari.</summary>
    Patron = 2,

    /// <summary>Depo personeli: stok, ürün girişi, satış.</summary>
    Depocu = 3
}
