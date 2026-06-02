using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Catalog.Categories;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetAllAsync(PageQuery query, CancellationToken ct = default);
    Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _db;

    public CategoryService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CategoryDto>> GetAllAsync(PageQuery query, CancellationToken ct = default)
    {
        var q = _db.Categories.AsNoTracking().OrderBy(c => c.Name).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(c => c.Name.Contains(query.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.IsActive))
            .ToListAsync(ct);

        return new PagedResult<CategoryDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return entity is null
            ? Result.Failure<CategoryDto>(Error.NotFound("Kategori bulunamadı."))
            : Result.Success(Map(entity));
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var entity = new Category { Name = request.Name.Trim(), Description = request.Description };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success(Map(entity));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return Result.Failure<CategoryDto>(Error.NotFound("Kategori bulunamadı."));

        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(Map(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Kategori bulunamadı."));

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id, ct);
        if (hasProducts)
            return Result.Failure(Error.Conflict("Bu kategoriye bağlı ürünler var, silinemez."));

        _db.Categories.Remove(entity); // interceptor soft delete uygular
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static CategoryDto Map(Category c) => new(c.Id, c.Name, c.Description, c.IsActive);
}
