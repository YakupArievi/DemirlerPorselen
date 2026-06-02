using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Infrastructure.Persistence;

namespace Toptanci.Infrastructure.Security;

/// <summary>
/// SQL Server sequence kullanarak benzersiz barkod üretir.
/// Şema: tek bir sıra numarası → adet için "A" + 12 hane, koli için "K" + 12 hane.
/// Farklı prefix'ler sayesinde adet/koli barkodları çakışmaz.
/// </summary>
public sealed class BarcodeGenerator : IBarcodeGenerator
{
    private readonly ApplicationDbContext _context;

    public BarcodeGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(string AdetBarcode, string KoliBarcode)> GenerateForVariantAsync(
        CancellationToken cancellationToken = default)
    {
        var next = await NextSequenceValueAsync(cancellationToken);
        var serial = next.ToString("D12");
        return ($"A{serial}", $"K{serial}");
    }

    private async Task<long> NextSequenceValueAsync(CancellationToken cancellationToken)
    {
        // SqlQueryRaw sorguyu bir alt-sorguya sarar; NEXT VALUE FOR alt-sorguda yasaktır.
        // Bu yüzden doğrudan ADO komutuyla çalıştırıyoruz.
        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT NEXT VALUE FOR [{ApplicationDbContext.BarcodeSequenceName}]";

            if (_context.Database.CurrentTransaction is { } transaction)
                command.Transaction = transaction.GetDbTransaction();

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }
}
