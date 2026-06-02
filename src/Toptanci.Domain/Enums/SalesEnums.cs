namespace Toptanci.Domain.Enums;

/// <summary>Cari hesap hareketi tipleri.</summary>
public enum AccountTransactionType
{
    Satis = 1,
    Tahsilat = 2,
    Iade = 3,
    BorcDuzeltme = 4,
    Iskonto = 5
}

/// <summary>Borç/alacak yönü. Borç bakiyeyi artırır (müşteri bize borçlanır), alacak azaltır.</summary>
public enum AccountDirection
{
    Borc = 1,
    Alacak = 2
}

/// <summary>Ödeme/tahsilat tipi.</summary>
public enum PaymentType
{
    Nakit = 1,
    Kart = 2,
    Cek = 3
}

public enum SaleStatus
{
    Active = 1,
    Cancelled = 2
}
