using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Stock;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Operations.Transfers;

public sealed record TransferLineRequest(Guid VariantId, int Quantity);

public sealed record CreateTransferRequest(
    Guid SourceWarehouseId, Guid TargetWarehouseId,
    IReadOnlyList<TransferLineRequest> Lines, DateTime? TransferDate, string? IdempotencyKey, string? Note);

public sealed record TransferLineDto(Guid VariantId, string ProductName, int Quantity);

public sealed record TransferDto(
    Guid Id, Guid SourceWarehouseId, string SourceWarehouseName,
    Guid TargetWarehouseId, string TargetWarehouseName,
    DateTime TransferDate, string? Note, IReadOnlyList<TransferLineDto> Lines);

public sealed class CreateTransferValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferValidator()
    {
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.TargetWarehouseId).NotEmpty()
            .NotEqual(x => x.SourceWarehouseId).WithMessage("Kaynak ve hedef depo aynı olamaz.");
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.VariantId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public interface IWarehouseTransferService
{
    Task<Result<TransferDto>> TransferAsync(CreateTransferRequest request, CancellationToken ct = default);
    Task<Result<TransferDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<TransferDto>> GetAllAsync(PageQuery query, CancellationToken ct = default);
}

public sealed class WarehouseTransferService : IWarehouseTransferService
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _stock;

    public WarehouseTransferService(IApplicationDbContext db, IStockLedger stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task<Result<TransferDto>> TransferAsync(CreateTransferRequest request, CancellationToken ct = default)
    {
        if (request.SourceWarehouseId == request.TargetWarehouseId)
            return Result.Failure<TransferDto>(Error.Validation("Kaynak ve hedef depo aynı olamaz."));

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _db.WarehouseTransfers.AsNoTracking()
                .Where(t => t.IdempotencyKey == request.IdempotencyKey)
                .Select(t => t.Id).FirstOrDefaultAsync(ct);
            if (existing != Guid.Empty)
                return await GetByIdAsync(existing, ct);
        }

        var warehouseIds = new[] { request.SourceWarehouseId, request.TargetWarehouseId };
        var existingWarehouses = await _db.Warehouses.CountAsync(w => warehouseIds.Contains(w.Id), ct);
        if (existingWarehouses != 2)
            return Result.Failure<TransferDto>(Error.Validation("Geçersiz depo."));

        var variantIds = request.Lines.Select(l => l.VariantId).Distinct().ToList();
        if (await _db.ProductVariants.CountAsync(v => variantIds.Contains(v.Id), ct) != variantIds.Count)
            return Result.Failure<TransferDto>(Error.Validation("Bir veya daha fazla varyant bulunamadı."));

        var transfer = new WarehouseTransfer
        {
            SourceWarehouseId = request.SourceWarehouseId,
            TargetWarehouseId = request.TargetWarehouseId,
            TransferDate = request.TransferDate ?? DateTime.UtcNow,
            IdempotencyKey = request.IdempotencyKey,
            Note = request.Note
        };

        foreach (var l in request.Lines)
        {
            transfer.Lines.Add(new WarehouseTransferLine { VariantId = l.VariantId, Quantity = l.Quantity });

            await _stock.ApplyAsync(new StockMovementInput(
                StockMovementType.DepoTransfer, l.VariantId, request.SourceWarehouseId, -l.Quantity,
                ReferenceType: "WarehouseTransfer", ReferenceId: transfer.Id, Note: "Transfer çıkış"), ct);

            await _stock.ApplyAsync(new StockMovementInput(
                StockMovementType.DepoTransfer, l.VariantId, request.TargetWarehouseId, l.Quantity,
                ReferenceType: "WarehouseTransfer", ReferenceId: transfer.Id, Note: "Transfer giriş"), ct);
        }

        _db.WarehouseTransfers.Add(transfer);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(transfer.Id, ct);
    }

    public async Task<Result<TransferDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.WarehouseTransfers.AsNoTracking()
            .Include(x => x.SourceWarehouse)
            .Include(x => x.TargetWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Variant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (t is null)
            return Result.Failure<TransferDto>(Error.NotFound("Transfer bulunamadı."));

        return Result.Success(new TransferDto(
            t.Id, t.SourceWarehouseId, t.SourceWarehouse.Name, t.TargetWarehouseId, t.TargetWarehouse.Name,
            t.TransferDate, t.Note,
            t.Lines.Select(l => new TransferLineDto(l.VariantId, l.Variant.Product.Name, l.Quantity)).ToList()));
    }

    public async Task<PagedResult<TransferDto>> GetAllAsync(PageQuery query, CancellationToken ct = default)
    {
        var q = _db.WarehouseTransfers.AsNoTracking().OrderByDescending(t => t.TransferDate);
        var total = await q.CountAsync(ct);
        var ids = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(t => t.Id).ToListAsync(ct);

        var items = new List<TransferDto>(ids.Count);
        foreach (var id in ids)
        {
            var dto = await GetByIdAsync(id, ct);
            if (dto.IsSuccess) items.Add(dto.Value);
        }

        return new PagedResult<TransferDto>(items, total, query.Page, query.PageSize);
    }
}
