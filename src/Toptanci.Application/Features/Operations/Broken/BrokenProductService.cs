using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Stock;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Operations.Broken;

public sealed record CreateBrokenProductRequest(
    Guid VariantId, Guid WarehouseId, int Quantity, DateTime? RecordDate, string? Description, string? PhotoUrl);

public sealed record BrokenProductDto(
    Guid Id, Guid VariantId, string ProductName, Guid WarehouseId, string WarehouseName,
    int Quantity, DateTime RecordDate, string? Description, string? PhotoUrl);

public sealed record BrokenQuery : PageQuery
{
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public sealed class CreateBrokenValidator : AbstractValidator<CreateBrokenProductRequest>
{
    public CreateBrokenValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public interface IBrokenProductService
{
    Task<Result<BrokenProductDto>> CreateAsync(CreateBrokenProductRequest request, CancellationToken ct = default);
    Task<PagedResult<BrokenProductDto>> GetAllAsync(BrokenQuery query, CancellationToken ct = default);
}

public sealed class BrokenProductService : IBrokenProductService
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _stock;

    public BrokenProductService(IApplicationDbContext db, IStockLedger stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task<Result<BrokenProductDto>> CreateAsync(CreateBrokenProductRequest request, CancellationToken ct = default)
    {
        if (!await _db.ProductVariants.AnyAsync(v => v.Id == request.VariantId, ct))
            return Result.Failure<BrokenProductDto>(Error.Validation("Geçersiz varyant."));
        if (!await _db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, ct))
            return Result.Failure<BrokenProductDto>(Error.Validation("Geçersiz depo."));

        var record = new BrokenProductRecord
        {
            VariantId = request.VariantId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            RecordDate = request.RecordDate ?? DateTime.UtcNow,
            Description = request.Description,
            PhotoUrl = request.PhotoUrl
        };
        _db.BrokenProductRecords.Add(record);

        await _stock.ApplyAsync(new StockMovementInput(
            StockMovementType.Kirik, request.VariantId, request.WarehouseId, -request.Quantity,
            ReferenceType: "BrokenProduct", ReferenceId: record.Id, Note: request.Description), ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(record.Id, ct);
    }

    public async Task<PagedResult<BrokenProductDto>> GetAllAsync(BrokenQuery query, CancellationToken ct = default)
    {
        var q = _db.BrokenProductRecords.AsNoTracking().AsQueryable();
        if (query.VariantId is { } vid) q = q.Where(b => b.VariantId == vid);
        if (query.WarehouseId is { } wid) q = q.Where(b => b.WarehouseId == wid);
        if (query.FromDate is { } f) q = q.Where(b => b.RecordDate >= f);
        if (query.ToDate is { } t) q = q.Where(b => b.RecordDate <= t);

        q = q.OrderByDescending(b => b.RecordDate);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BrokenProductDto(
                b.Id, b.VariantId, b.Variant.Product.Name, b.WarehouseId, b.Warehouse.Name,
                b.Quantity, b.RecordDate, b.Description, b.PhotoUrl))
            .ToListAsync(ct);

        return new PagedResult<BrokenProductDto>(items, total, query.Page, query.PageSize);
    }

    private async Task<Result<BrokenProductDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var dto = await _db.BrokenProductRecords.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BrokenProductDto(
                b.Id, b.VariantId, b.Variant.Product.Name, b.WarehouseId, b.Warehouse.Name,
                b.Quantity, b.RecordDate, b.Description, b.PhotoUrl))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<BrokenProductDto>(Error.NotFound("Kayıt bulunamadı."))
            : Result.Success(dto);
    }
}
