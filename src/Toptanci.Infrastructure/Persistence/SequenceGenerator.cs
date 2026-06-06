using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Infrastructure.Persistence;

/// <summary>
/// Bir DB sequence'inden sonraki değeri ham ADO ile alır. Provider-bağımsız:
///   - SQL Server: SELECT NEXT VALUE FOR [seq]
///   - PostgreSQL: SELECT nextval('"seq"')
/// (EF SqlQueryRaw sorguyu alt-sorguya sarar; bu ifadeler alt-sorguda yasaktır, bu yüzden ham komut.)
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
            command.CommandText = _context.Database.IsNpgsql()
                ? $"SELECT nextval('\"{sequenceName}\"')"
                : $"SELECT NEXT VALUE FOR [{sequenceName}]";

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
