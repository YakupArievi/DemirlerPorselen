using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Stock;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Operations.Counts;

public sealed record CreateStockCountRequest(Guid WarehouseId, DateTime? CountDate, string? Note);
public sealed record CountLineRequest(Guid VariantId, int CountedQuantity);

public sealed record StockCountLineDto(
    Guid Id, Guid VariantId, string ProductName, string AdetBarcode,
    int SystemQuantity, int CountedQuantity, int Difference);

public sealed record StockCountDto(
    Guid Id, Guid WarehouseId, string WarehouseName, DateTime CountDate,
    StockCountStatus Status, DateTime? ApprovedAt, string? Note,
    IReadOnlyList<StockCountLineDto> Lines);

public sealed class CreateStockCountValidator : AbstractValidator<CreateStockCountRequest>
{
    public CreateStockCountValidator() => RuleFor(x => x.WarehouseId).NotEmpty();
}

public sealed class CountLineValidator : AbstractValidator<CountLineRequest>
{
    public CountLineValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0);
    }
}

public interface IStockCountService
{
    Task<Result<StockCountDto>> CreateAsync(CreateStockCountRequest request, CancellationToken ct = default);
    Task<Result<StockCountDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<StockCountDto>> UpsertLineAsync(Guid countId, CountLineRequest request, CancellationToken ct = default);
    Task<Result<StockCountDto>> ApproveAsync(Guid countId, CancellationToken ct = default);
    Task<Result> CancelAsync(Guid countId, CancellationToken ct = default);
}

public sealed class StockCountService : IStockCountService
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _stock;

    public StockCountService(IApplicationDbContext db, IStockLedger stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task<Result<StockCountDto>> CreateAsync(CreateStockCountRequest request, CancellationToken ct = default)
    {
        if (!await _db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, ct))
            return Result.Failure<StockCountDto>(Error.Validation("Geçersiz depo."));

        var count = new StockCount
        {
            WarehouseId = request.WarehouseId,
            CountDate = request.CountDate ?? DateTime.UtcNow,
            Status = StockCountStatus.Draft,
            Note = request.Note
        };
        _db.StockCounts.Add(count);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(count.Id, ct);
    }

    public async Task<Result<StockCountDto>> UpsertLineAsync(Guid countId, CountLineRequest request, CancellationToken ct = default)
    {
        var count = await _db.StockCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == countId, ct);
        if (count is null)
            return Result.Failure<StockCountDto>(Error.NotFound("Sayım fişi bulunamadı."));
        if (count.Status != StockCountStatus.Draft)
            return Result.Failure<StockCountDto>(Error.Conflict("Yalnızca taslak sayım fişi düzenlenebilir."));
        if (!await _db.ProductVariants.AnyAsync(v => v.Id == request.VariantId, ct))
            return Result.Failure<StockCountDto>(Error.Validation("Geçersiz varyant."));

        // Satırı doğrudan ekle/güncelle (parent'a dokunma → gereksiz concurrency güncellemesi olmaz)
        var line = await _db.StockCountLines
            .FirstOrDefaultAsync(l => l.StockCountId == countId && l.VariantId == request.VariantId, ct);
        if (line is null)
            _db.StockCountLines.Add(new StockCountLine { StockCountId = countId, VariantId = request.VariantId, CountedQuantity = request.CountedQuantity });
        else
            line.CountedQuantity = request.CountedQuantity;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(countId, ct);
    }

    public async Task<Result<StockCountDto>> ApproveAsync(Guid countId, CancellationToken ct = default)
    {
        var count = await _db.StockCounts.Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == countId, ct);
        if (count is null)
            return Result.Failure<StockCountDto>(Error.NotFound("Sayım fişi bulunamadı."));
        if (count.Status != StockCountStatus.Draft)
            return Result.Failure<StockCountDto>(Error.Conflict("Sayım fişi zaten kapatılmış."));
        if (count.Lines.Count == 0)
            return Result.Failure<StockCountDto>(Error.Validation("Sayım fişinde satır yok."));

        var variantIds = count.Lines.Select(l => l.VariantId).ToList();
        var systemQty = await _db.StockItems.AsNoTracking()
            .Where(s => s.WarehouseId == count.WarehouseId && variantIds.Contains(s.VariantId))
            .ToDictionaryAsync(s => s.VariantId, s => s.Quantity, ct);

        foreach (var line in count.Lines)
        {
            var current = systemQty.GetValueOrDefault(line.VariantId, 0);
            line.Difference = line.CountedQuantity - current;

            if (line.Difference != 0)
            {
                await _stock.ApplyAsync(new StockMovementInput(
                    StockMovementType.SayimFarki, line.VariantId, count.WarehouseId, line.Difference,
                    ReferenceType: "StockCount", ReferenceId: count.Id, Note: "Sayım farkı"), ct);
            }
        }

        count.Status = StockCountStatus.Approved;
        count.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(countId, ct);
    }

    public async Task<Result> CancelAsync(Guid countId, CancellationToken ct = default)
    {
        var count = await _db.StockCounts.FirstOrDefaultAsync(c => c.Id == countId, ct);
        if (count is null)
            return Result.Failure(Error.NotFound("Sayım fişi bulunamadı."));
        if (count.Status == StockCountStatus.Approved)
            return Result.Failure(Error.Conflict("Onaylanmış sayım iptal edilemez."));

        count.Status = StockCountStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<StockCountDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var count = await _db.StockCounts.AsNoTracking()
            .Include(c => c.Warehouse)
            .Include(c => c.Lines).ThenInclude(l => l.Variant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (count is null)
            return Result.Failure<StockCountDto>(Error.NotFound("Sayım fişi bulunamadı."));

        // Taslak için güncel sistem miktarını canlı göster (fark önizleme)
        var variantIds = count.Lines.Select(l => l.VariantId).ToList();
        var systemQty = await _db.StockItems.AsNoTracking()
            .Where(s => s.WarehouseId == count.WarehouseId && variantIds.Contains(s.VariantId))
            .ToDictionaryAsync(s => s.VariantId, s => s.Quantity, ct);

        var lines = count.Lines.Select(l =>
        {
            var system = systemQty.GetValueOrDefault(l.VariantId, 0);
            var diff = count.Status == StockCountStatus.Approved ? l.Difference : l.CountedQuantity - system;
            return new StockCountLineDto(l.Id, l.VariantId, l.Variant.Product.Name, l.Variant.AdetBarcode,
                system, l.CountedQuantity, diff);
        }).ToList();

        return Result.Success(new StockCountDto(
            count.Id, count.WarehouseId, count.Warehouse.Name, count.CountDate,
            count.Status, count.ApprovedAt, count.Note, lines));
    }
}
