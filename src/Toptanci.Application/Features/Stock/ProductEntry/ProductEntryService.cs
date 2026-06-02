using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Stock.ProductEntry;

public interface IProductEntryService
{
    Task<Result<ProductEntryResultDto>> ReceiveAsync(ProductEntryRequest request, CancellationToken ct = default);
}

public sealed class ProductEntryService : IProductEntryService
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;

    public ProductEntryService(IApplicationDbContext db, IStockLedger ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<Result<ProductEntryResultDto>> ReceiveAsync(ProductEntryRequest request, CancellationToken ct = default)
    {
        if (!await _db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, ct))
            return Result.Failure<ProductEntryResultDto>(Error.Validation("Geçersiz depo."));

        var variantIds = request.Lines.Select(l => l.VariantId).Distinct().ToList();
        var variants = await _db.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        if (variants.Count != variantIds.Count)
            return Result.Failure<ProductEntryResultDto>(Error.Validation("Bir veya daha fazla varyant bulunamadı."));

        // Bu varyantların mevcut toplam adetleri (ağırlıklı ortalama maliyet için)
        var currentTotals = await _db.StockItems
            .Where(s => variantIds.Contains(s.VariantId))
            .GroupBy(s => s.VariantId)
            .Select(g => new { VariantId = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.VariantId, x => x.Total, ct);

        var totalAdet = 0;
        var anyApplied = false;

        foreach (var line in request.Lines)
        {
            var variant = variants[line.VariantId];

            var adetQty = line.UnitType == UnitType.Koli
                ? line.Quantity * variant.KoliIciAdet
                : line.Quantity;

            // Birim maliyeti adet bazına indir
            var perAdetCost = line.UnitType == UnitType.Koli && variant.KoliIciAdet > 0
                ? line.UnitPurchasePrice / variant.KoliIciAdet
                : line.UnitPurchasePrice;

            // Ağırlıklı ortalama alış maliyeti
            var currentQty = currentTotals.GetValueOrDefault(variant.Id, 0);
            variant.AverageCost = currentQty > 0
                ? (currentQty * variant.AverageCost + adetQty * perAdetCost) / (currentQty + adetQty)
                : perAdetCost;
            variant.PurchasePrice = perAdetCost; // en son alış fiyatı referansı

            // Sonraki satırlar için cache'lenmiş toplamı güncelle
            currentTotals[variant.Id] = currentQty + adetQty;

            var idemKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? null
                : $"{request.IdempotencyKey}:{variant.Id}";

            var movement = await _ledger.ApplyAsync(new StockMovementInput(
                StockMovementType.Giris,
                variant.Id,
                request.WarehouseId,
                adetQty,
                perAdetCost,
                ReferenceType: "ProductEntry",
                IdempotencyKey: idemKey,
                Note: request.Note), ct);

            if (movement is not null)
            {
                anyApplied = true;
                totalAdet += adetQty;
            }
        }

        if (!anyApplied)
            return Result.Success(new ProductEntryResultDto(request.Lines.Count, 0, AlreadyProcessed: true));

        await _db.SaveChangesAsync(ct);
        return Result.Success(new ProductEntryResultDto(request.Lines.Count, totalAdet, AlreadyProcessed: false));
    }
}
