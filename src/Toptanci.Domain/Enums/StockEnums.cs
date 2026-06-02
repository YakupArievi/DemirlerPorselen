namespace Toptanci.Domain.Enums;

/// <summary>Stok birim tipi. Temel birim her zaman adettir; koli = KoliIciAdet × adet.</summary>
public enum UnitType
{
    Adet = 1,
    Koli = 2
}

/// <summary>Stok hareketi tipleri. Quantity işaretlidir (+ giriş, - çıkış).</summary>
public enum StockMovementType
{
    Giris = 1,
    Satis = 2,
    SatisIptali = 3,
    Iade = 4,
    DepoTransfer = 5,
    Kirik = 6,
    ManuelDuzeltme = 7,
    SayimFarki = 8
}
