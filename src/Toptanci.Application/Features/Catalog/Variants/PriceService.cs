using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;

namespace Toptanci.Application.Features.Catalog.Variants;

public interface IPriceService
{
    Task<Result<VariantDto>> ChangePriceAsync(Guid variantId, ChangePriceRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PriceHistoryDto>>> GetHistoryAsync(Guid variantId, CancellationToken ct = default);
}

public sealed class PriceService : IPriceService
{
    private readonly IApplicationDbContext _db;

    public PriceService(IApplicationDbContext db) => _db = db;

    public async Task<Result<VariantDto>> ChangePriceAsync(Guid variantId, ChangePriceRequest request, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, ct);
        if (variant is null)
            return Result.Failure<VariantDto>(Error.NotFound("Varyant bulunamadı."));

        // Değişiklik yoksa geçmişe kayıt atma
        if (variant.PurchasePrice == request.PurchasePrice && variant.SalePrice == request.SalePrice)
            return Result.Failure<VariantDto>(Error.Validation("Fiyatlarda değişiklik yok."));

        _db.PriceHistories.Add(new PriceHistory
        {
            VariantId = variant.Id,
            OldPurchasePrice = variant.PurchasePrice,
            NewPurchasePrice = request.PurchasePrice,
            OldSalePrice = variant.SalePrice,
            NewSalePrice = request.SalePrice
        });

        variant.PurchasePrice = request.PurchasePrice;
        variant.SalePrice = request.SalePrice;

        await _db.SaveChangesAsync(ct);

        var dto = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => new VariantDto(
                v.Id, v.ProductId, v.Product.Name, v.Color, v.Pattern, v.Size,
                v.AdetBarcode, v.KoliBarcode, v.KoliIciAdet,
                v.PurchasePrice, v.SalePrice, v.AverageCost, v.MinStock, v.ImageUrl, v.IsActive))
            .FirstAsync(ct);

        return Result.Success(dto);
    }

    public async Task<Result<IReadOnlyList<PriceHistoryDto>>> GetHistoryAsync(Guid variantId, CancellationToken ct = default)
    {
        if (!await _db.ProductVariants.AnyAsync(v => v.Id == variantId, ct))
            return Result.Failure<IReadOnlyList<PriceHistoryDto>>(Error.NotFound("Varyant bulunamadı."));

        var items = await _db.PriceHistories.AsNoTracking()
            .Where(p => p.VariantId == variantId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PriceHistoryDto(
                p.Id, p.VariantId, p.OldPurchasePrice, p.NewPurchasePrice,
                p.OldSalePrice, p.NewSalePrice, p.CreatedAt, p.CreatedBy))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<PriceHistoryDto>>(items);
    }
}
