using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Catalog.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query, CancellationToken ct = default);
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;

    public ProductService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query, CancellationToken ct = default)
    {
        var q = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.Contains(query.Search));
        if (query.CategoryId is { } cid)
            q = q.Where(p => p.CategoryId == cid);
        if (query.BrandId is { } bid)
            q = q.Where(p => p.BrandId == bid);
        if (query.IsActive is { } active)
            q = q.Where(p => p.IsActive == active);

        q = q.OrderBy(p => p.Name);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description,
                p.CategoryId, p.Category.Name,
                p.BrandId, p.Brand != null ? p.Brand.Name : null,
                p.IsActive, p.Variants.Count))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await _db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description,
                p.CategoryId, p.Category.Name,
                p.BrandId, p.Brand != null ? p.Brand.Name : null,
                p.IsActive, p.Variants.Count))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<ProductDto>(Error.NotFound("Ürün bulunamadı."))
            : Result.Success(dto);
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var validation = await ValidateRefsAsync(request.CategoryId, request.BrandId, ct);
        if (validation is not null)
            return Result.Failure<ProductDto>(validation);

        var entity = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId
        };
        _db.Products.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return Result.Failure<ProductDto>(Error.NotFound("Ürün bulunamadı."));

        var validation = await ValidateRefsAsync(request.CategoryId, request.BrandId, ct);
        if (validation is not null)
            return Result.Failure<ProductDto>(validation);

        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        entity.CategoryId = request.CategoryId;
        entity.BrandId = request.BrandId;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Ürün bulunamadı."));

        _db.Products.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Error?> ValidateRefsAsync(Guid categoryId, Guid? brandId, CancellationToken ct)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            return Error.Validation("Geçersiz kategori.");
        if (brandId is { } bid && !await _db.Brands.AnyAsync(b => b.Id == bid, ct))
            return Error.Validation("Geçersiz marka.");
        return null;
    }
}
