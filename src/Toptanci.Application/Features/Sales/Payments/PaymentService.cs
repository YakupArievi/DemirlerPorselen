using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales.Payments;

public interface IPaymentService
{
    Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<PagedResult<PaymentDto>> GetAllAsync(PaymentQuery query, CancellationToken ct = default);
}

public sealed class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountLedger _account;

    public PaymentService(IApplicationDbContext db, IAccountLedger account)
    {
        _db = db;
        _account = account;
    }

    public async Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingId = await _db.Payments.AsNoTracking()
                .Where(p => p.IdempotencyKey == request.IdempotencyKey)
                .Select(p => p.Id).FirstOrDefaultAsync(ct);
            if (existingId != Guid.Empty)
                return await GetByIdAsync(existingId, ct);
        }

        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            return Result.Failure<PaymentDto>(Error.Validation("Geçersiz müşteri."));

        Sale? sale = null;
        if (request.SaleId is { } saleId)
        {
            sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == saleId, ct);
            if (sale is null)
                return Result.Failure<PaymentDto>(Error.Validation("Geçersiz satış."));
            if (sale.CustomerId != request.CustomerId)
                return Result.Failure<PaymentDto>(Error.Validation("Satış bu müşteriye ait değil."));
        }

        var payment = new Payment
        {
            CustomerId = request.CustomerId,
            SaleId = request.SaleId,
            Type = request.Type,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            DueDate = request.DueDate,
            IdempotencyKey = request.IdempotencyKey,
            Note = request.Note
        };
        _db.Payments.Add(payment);

        if (sale is not null)
            sale.PaidAmount += request.Amount;

        await _account.PostAsync(new AccountTransactionInput(
            AccountTransactionType.Tahsilat, request.CustomerId, AccountDirection.Alacak, request.Amount,
            ReferenceType: "Payment", ReferenceId: payment.Id, Note: request.Note), ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(payment.Id, ct);
    }

    public async Task<PagedResult<PaymentDto>> GetAllAsync(PaymentQuery query, CancellationToken ct = default)
    {
        var q = _db.Payments.AsNoTracking().AsQueryable();

        if (query.CustomerId is { } cid) q = q.Where(p => p.CustomerId == cid);
        if (query.Type is { } type) q = q.Where(p => p.Type == type);
        if (query.FromDate is { } f) q = q.Where(p => p.PaymentDate >= f);
        if (query.ToDate is { } t) q = q.Where(p => p.PaymentDate <= t);

        q = q.OrderByDescending(p => p.PaymentDate);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PaymentDto(p.Id, p.CustomerId, p.Customer.Name, p.SaleId, p.Type, p.Amount, p.PaymentDate, p.DueDate, p.Note))
            .ToListAsync(ct);

        return new PagedResult<PaymentDto>(items, total, query.Page, query.PageSize);
    }

    private async Task<Result<PaymentDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var dto = await _db.Payments.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PaymentDto(p.Id, p.CustomerId, p.Customer.Name, p.SaleId, p.Type, p.Amount, p.PaymentDate, p.DueDate, p.Note))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<PaymentDto>(Error.NotFound("Ödeme bulunamadı."))
            : Result.Success(dto);
    }
}
