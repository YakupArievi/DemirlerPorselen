using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales.Accounts;

public sealed record AccountStatementLineDto(
    DateTime Date,
    AccountTransactionType Type,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note);

public sealed record AccountStatementDto(
    Guid CustomerId,
    string CustomerName,
    DateTime FromDate,
    DateTime ToDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<AccountStatementLineDto> Lines);
