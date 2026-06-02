using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Application.Features.Stock.Queries;

public interface IStockQueryService
{
    Task<IReadOnlyList<StockLevelDto>> GetByVariantAsync(Guid variantId, CancellationToken ct = default);
    Task<PagedResult<StockLevelDto>> GetByWarehouseAsync(Guid warehouseId, PageQuery query, CancellationToken ct = default);
    Task<PagedResult<StockMovementDto>> GetMovementsAsync(MovementQuery query, CancellationToken ct = default);
}

public sealed class StockQueryService : IStockQueryService
{
    private readonly IApplicationDbContext _db;

    public StockQueryService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockLevelDto>> GetByVariantAsync(Guid variantId, CancellationToken ct = default)
        => await _db.StockItems.AsNoTracking()
            .Where(s => s.VariantId == variantId)
            .Select(s => new StockLevelDto(
                s.VariantId, s.Variant.Product.Name,
                s.Variant.Color, s.Variant.Pattern, s.Variant.Size, s.Variant.AdetBarcode,
                s.WarehouseId, s.Warehouse.Name,
                s.Quantity, s.Variant.MinStock, s.Quantity < s.Variant.MinStock))
            .ToListAsync(ct);

    public async Task<PagedResult<StockLevelDto>> GetByWarehouseAsync(Guid warehouseId, PageQuery query, CancellationToken ct = default)
    {
        var q = _db.StockItems.AsNoTracking().Where(s => s.WarehouseId == warehouseId);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(s => s.Variant.Product.Name.Contains(query.Search)
                          || s.Variant.AdetBarcode == query.Search
                          || s.Variant.KoliBarcode == query.Search);

        q = q.OrderBy(s => s.Variant.Product.Name);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new StockLevelDto(
                s.VariantId, s.Variant.Product.Name,
                s.Variant.Color, s.Variant.Pattern, s.Variant.Size, s.Variant.AdetBarcode,
                s.WarehouseId, s.Warehouse.Name,
                s.Quantity, s.Variant.MinStock, s.Quantity < s.Variant.MinStock))
            .ToListAsync(ct);

        return new PagedResult<StockLevelDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(MovementQuery query, CancellationToken ct = default)
    {
        var q = _db.StockMovements.AsNoTracking().AsQueryable();

        if (query.VariantId is { } vid) q = q.Where(m => m.VariantId == vid);
        if (query.WarehouseId is { } wid) q = q.Where(m => m.WarehouseId == wid);
        if (query.Type is { } type) q = q.Where(m => m.Type == type);
        if (query.FromDate is { } from) q = q.Where(m => m.CreatedAt >= from);
        if (query.ToDate is { } to) q = q.Where(m => m.CreatedAt <= to);

        q = q.OrderByDescending(m => m.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new StockMovementDto(
                m.Id, m.Type, m.VariantId, m.Variant.Product.Name,
                m.WarehouseId, m.Warehouse.Name,
                m.Quantity, m.UnitCost, m.ReferenceType, m.ReferenceId, m.Note,
                m.CreatedAt, m.CreatedBy))
            .ToListAsync(ct);

        return new PagedResult<StockMovementDto>(items, total, query.Page, query.PageSize);
    }
}
