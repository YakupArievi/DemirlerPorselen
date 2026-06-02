using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales.Accounts;

public interface IAccountService
{
    Task<Result<decimal>> GetBalanceAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<AccountStatementDto>> GetStatementAsync(Guid customerId, DateTime? from, DateTime? to, CancellationToken ct = default);
}

public sealed class AccountService : IAccountService
{
    private readonly IApplicationDbContext _db;

    public AccountService(IApplicationDbContext db) => _db = db;

    public async Task<Result<decimal>> GetBalanceAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Balance })
            .FirstOrDefaultAsync(ct);

        return customer is null
            ? Result.Failure<decimal>(Error.NotFound("Müşteri bulunamadı."))
            : Result.Success(customer.Balance);
    }

    public async Task<Result<AccountStatementDto>> GetStatementAsync(Guid customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Id, c.Name, c.OpeningBalance })
            .FirstOrDefaultAsync(ct);

        if (customer is null)
            return Result.Failure<AccountStatementDto>(Error.NotFound("Müşteri bulunamadı."));

        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        // Dönem başı bakiyesi = açılış + dönem öncesi hareketlerin işaretli toplamı
        var beforeTxns = await _db.AccountTransactions.AsNoTracking()
            .Where(t => t.CustomerId == customerId && t.CreatedAt < fromDate)
            .Select(t => new { t.Direction, t.Amount })
            .ToListAsync(ct);
        var openingBalance = customer.OpeningBalance
            + beforeTxns.Sum(t => t.Direction == AccountDirection.Borc ? t.Amount : -t.Amount);

        var periodTxns = await _db.AccountTransactions.AsNoTracking()
            .Where(t => t.CustomerId == customerId && t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Type)
            .Select(t => new { t.CreatedAt, t.Type, t.Direction, t.Amount, t.ReferenceType, t.ReferenceId, t.Note })
            .ToListAsync(ct);

        var lines = new List<AccountStatementLineDto>(periodTxns.Count);
        var running = openingBalance;
        decimal totalDebit = 0, totalCredit = 0;

        foreach (var t in periodTxns)
        {
            var debit = t.Direction == AccountDirection.Borc ? t.Amount : 0m;
            var credit = t.Direction == AccountDirection.Alacak ? t.Amount : 0m;
            running += debit - credit;
            totalDebit += debit;
            totalCredit += credit;
            lines.Add(new AccountStatementLineDto(t.CreatedAt, t.Type, debit, credit, running, t.ReferenceType, t.ReferenceId, t.Note));
        }

        return Result.Success(new AccountStatementDto(
            customer.Id, customer.Name, fromDate, toDate,
            openingBalance, running, totalDebit, totalCredit, lines));
    }
}
