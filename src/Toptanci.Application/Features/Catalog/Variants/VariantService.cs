using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Catalog.Variants;

public interface IVariantService
{
    Task<PagedResult<VariantDto>> GetAllAsync(PageQuery query, CancellationToken ct = default);
    Task<Result<IReadOnlyList<VariantDto>>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task<Result<VariantDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<VariantDto>> CreateAsync(CreateVariantRequest request, CancellationToken ct = default);
    Task<Result<VariantDto>> UpdateAsync(Guid id, UpdateVariantRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ResolvedBarcodeDto>> ResolveBarcodeAsync(string barcode, CancellationToken ct = default);
}

public sealed class VariantService : IVariantService
{
    private readonly IApplicationDbContext _db;
    private readonly IBarcodeGenerator _barcodeGenerator;

    public VariantService(IApplicationDbContext db, IBarcodeGenerator barcodeGenerator)
    {
        _db = db;
        _barcodeGenerator = barcodeGenerator;
    }

    public async Task<PagedResult<VariantDto>> GetAllAsync(PageQuery query, CancellationToken ct = default)
    {
        var q = _db.ProductVariants.AsNoTracking().Where(v => v.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(v => v.Product.Name.Contains(query.Search) || v.AdetBarcode == query.Search || v.KoliBarcode == query.Search);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(v => v.Product.Name).ThenBy(v => v.Color)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync(ct);

        return new PagedResult<VariantDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Result<IReadOnlyList<VariantDto>>> GetByProductAsync(Guid productId, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            return Result.Failure<IReadOnlyList<VariantDto>>(Error.NotFound("Ürün bulunamadı."));

        var items = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Color).ThenBy(v => v.Size)
            .Select(ToDto)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VariantDto>>(items);
    }

    public async Task<Result<VariantDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<VariantDto>(Error.NotFound("Varyant bulunamadı."))
            : Result.Success(dto);
    }

    public async Task<Result<VariantDto>> CreateAsync(CreateVariantRequest request, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == request.ProductId, ct))
            return Result.Failure<VariantDto>(Error.Validation("Geçersiz ürün."));

        var (adetBarcode, koliBarcode) = await _barcodeGenerator.GenerateForVariantAsync(ct);

        var entity = new ProductVariant
        {
            ProductId = request.ProductId,
            Color = request.Color,
            Pattern = request.Pattern,
            Size = request.Size,
            AdetBarcode = adetBarcode,
            KoliBarcode = koliBarcode,
            KoliIciAdet = request.KoliIciAdet,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            AverageCost = request.PurchasePrice, // ilk maliyet = alış fiyatı
            MinStock = request.MinStock,
            ImageUrl = request.ImageUrl
        };
        _db.ProductVariants.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result<VariantDto>> UpdateAsync(Guid id, UpdateVariantRequest request, CancellationToken ct = default)
    {
        var entity = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
            return Result.Failure<VariantDto>(Error.NotFound("Varyant bulunamadı."));

        entity.Color = request.Color;
        entity.Pattern = request.Pattern;
        entity.Size = request.Size;
        entity.KoliIciAdet = request.KoliIciAdet;
        entity.MinStock = request.MinStock;
        entity.ImageUrl = request.ImageUrl;
        entity.IsActive = request.IsActive;
        // Fiyatlar burada DEĞİŞMEZ (yalnızca Patron, PriceService ile). Barkod ve AverageCost da değişmez.
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
            return Result.Failure(Error.NotFound("Varyant bulunamadı."));

        _db.ProductVariants.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ResolvedBarcodeDto>> ResolveBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        barcode = barcode.Trim();

        var variant = await _db.ProductVariants.AsNoTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.AdetBarcode == barcode || v.KoliBarcode == barcode, ct);

        if (variant is null)
            return Result.Failure<ResolvedBarcodeDto>(Error.NotFound("Barkod bulunamadı."));

        var unit = variant.AdetBarcode == barcode ? UnitType.Adet : UnitType.Koli;
        var adetEquivalent = unit == UnitType.Adet ? 1 : variant.KoliIciAdet;

        return Result.Success(new ResolvedBarcodeDto(
            variant.Id, variant.Product.Name,
            variant.Color, variant.Pattern, variant.Size,
            unit, adetEquivalent, variant.SalePrice));
    }

    private static readonly Expression<Func<ProductVariant, VariantDto>> ToDto = v => new VariantDto(
        v.Id, v.ProductId, v.Product.Name,
        v.Color, v.Pattern, v.Size,
        v.AdetBarcode, v.KoliBarcode, v.KoliIciAdet,
        v.PurchasePrice, v.SalePrice, v.AverageCost,
        v.MinStock, v.ImageUrl, v.IsActive);
}
