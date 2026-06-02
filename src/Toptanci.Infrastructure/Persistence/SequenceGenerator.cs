using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// Ham ADO ile "SELECT NEXT VALUE FOR [seq]" çalıştırır.
/// (EF SqlQueryRaw sorguyu alt-sorguya sarar; NEXT VALUE FOR alt-sorguda yasaktır.)
/// Açık bir transaction varsa ona katılır.
/// </summary>
public sealed class SequenceGenerator : ISequenceGenerator
{
    private readonly ApplicationDbContext _context;

    public SequenceGenerator(ApplicationDbContext context) => _context = context;

    public async Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT NEXT VALUE FOR [{sequenceName}]";

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
