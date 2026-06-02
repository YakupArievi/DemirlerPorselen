using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Stock.Warehouses;

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken ct = default);
    Task<Result<WarehouseDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, CancellationToken ct = default);
    Task<Result<WarehouseDto>> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class WarehouseService : IWarehouseService
{
    private readonly IApplicationDbContext _db;

    public WarehouseService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken ct = default)
        => await _db.Warehouses.AsNoTracking()
            .OrderByDescending(w => w.IsDefault).ThenBy(w => w.Name)
            .Select(w => new WarehouseDto(w.Id, w.Name, w.Code, w.IsDefault, w.IsActive))
            .ToListAsync(ct);

    public async Task<Result<WarehouseDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await _db.Warehouses.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new WarehouseDto(w.Id, w.Name, w.Code, w.IsDefault, w.IsActive))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<WarehouseDto>(Error.NotFound("Depo bulunamadı."))
            : Result.Success(dto);
    }

    public async Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, CancellationToken ct = default)
    {
        var entity = new Warehouse
        {
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            IsDefault = request.IsDefault
        };

        if (request.IsDefault)
            await ClearDefaultAsync(ct);

        _db.Warehouses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result<WarehouseDto>> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entity is null)
            return Result.Failure<WarehouseDto>(Error.NotFound("Depo bulunamadı."));

        if (request.IsDefault && !entity.IsDefault)
            await ClearDefaultAsync(ct);

        entity.Name = request.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Depo bulunamadı."));

        var hasStock = await _db.StockItems.AnyAsync(s => s.WarehouseId == id && s.Quantity != 0, ct);
        if (hasStock)
            return Result.Failure(Error.Conflict("Depoda stok bulunuyor, silinemez."));

        _db.Warehouses.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task ClearDefaultAsync(CancellationToken ct)
    {
        var current = await _db.Warehouses.Where(w => w.IsDefault).ToListAsync(ct);
        foreach (var w in current)
            w.IsDefault = false;
    }
}
