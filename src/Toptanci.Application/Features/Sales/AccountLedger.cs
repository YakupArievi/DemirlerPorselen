using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales;

public sealed record AccountTransactionInput(
    AccountTransactionType Type,
    Guid CustomerId,
    AccountDirection Direction,
    decimal Amount,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? IdempotencyKey = null,
    string? Note = null);

/// <summary>
/// Cari hesap ledger çekirdeği (mimari kural 7). AccountTransaction ekler ve müşteri bakiye
/// cache'ini günceller. SaveChanges ÇAĞIRMAZ — çağıran tek transaction içinde toplar.
/// </summary>
public interface IAccountLedger
{
    Task<AccountTransaction?> PostAsync(AccountTransactionInput input, CancellationToken ct = default);
}

public sealed class AccountLedger : IAccountLedger
{
    private readonly IApplicationDbContext _db;

    public AccountLedger(IApplicationDbContext db) => _db = db;

    public async Task<AccountTransaction?> PostAsync(AccountTransactionInput input, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(input.IdempotencyKey))
        {
            var already = await _db.AccountTransactions
                .AnyAsync(t => t.IdempotencyKey == input.IdempotencyKey, ct);
            if (already)
                return null;
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == input.CustomerId, ct);
        if (customer is null)
            throw new InvalidOperationException("Müşteri bulunamadı.");

        var transaction = new AccountTransaction
        {
            CustomerId = input.CustomerId,
            Type = input.Type,
            Direction = input.Direction,
            Amount = input.Amount,
            ReferenceType = input.ReferenceType,
            ReferenceId = input.ReferenceId,
            IdempotencyKey = input.IdempotencyKey,
            Note = input.Note
        };

        customer.Balance += transaction.SignedAmount;
        _db.AccountTransactions.Add(transaction);

        return transaction;
    }
}
