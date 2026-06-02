using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Catalog.Brands;

public interface IBrandService
{
    Task<PagedResult<BrandDto>> GetAllAsync(PageQuery query, CancellationToken ct = default);
    Task<Result<BrandDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<BrandDto>> CreateAsync(CreateBrandRequest request, CancellationToken ct = default);
    Task<Result<BrandDto>> UpdateAsync(Guid id, UpdateBrandRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class BrandService : IBrandService
{
    private readonly IApplicationDbContext _db;

    public BrandService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<BrandDto>> GetAllAsync(PageQuery query, CancellationToken ct = default)
    {
        var q = _db.Brands.AsNoTracking().OrderBy(b => b.Name).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(b => b.Name.Contains(query.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BrandDto(b.Id, b.Name, b.IsActive))
            .ToListAsync(ct);

        return new PagedResult<BrandDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Result<BrandDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return entity is null
            ? Result.Failure<BrandDto>(Error.NotFound("Marka bulunamadı."))
            : Result.Success(Map(entity));
    }

    public async Task<Result<BrandDto>> CreateAsync(CreateBrandRequest request, CancellationToken ct = default)
    {
        var entity = new Brand { Name = request.Name.Trim() };
        _db.Brands.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success(Map(entity));
    }

    public async Task<Result<BrandDto>> UpdateAsync(Guid id, UpdateBrandRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            return Result.Failure<BrandDto>(Error.NotFound("Marka bulunamadı."));

        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(Map(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Marka bulunamadı."));

        var hasProducts = await _db.Products.AnyAsync(p => p.BrandId == id, ct);
        if (hasProducts)
            return Result.Failure(Error.Conflict("Bu markaya bağlı ürünler var, silinemez."));

        _db.Brands.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static BrandDto Map(Brand b) => new(b.Id, b.Name, b.IsActive);
}
