using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Stock;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Sales.Sales;

public interface ISaleService
{
    Task<Result<SaleDto>> CreateAsync(CreateSaleRequest request, CancellationToken ct = default);
    Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SaleListItemDto>> GetAllAsync(SaleQuery query, CancellationToken ct = default);
    Task<Result> CancelAsync(Guid id, CancellationToken ct = default);
    Task<Result<SaleDto>> ReturnAsync(ReturnSaleRequest request, CancellationToken ct = default);
}

public sealed class SaleService : ISaleService
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _stock;
    private readonly IAccountLedger _account;
    private readonly ISequenceGenerator _sequence;

    public SaleService(IApplicationDbContext db, IStockLedger stock, IAccountLedger account, ISequenceGenerator sequence)
    {
        _db = db;
        _stock = stock;
        _account = account;
        _sequence = sequence;
    }

    public async Task<Result<SaleDto>> CreateAsync(CreateSaleRequest request, CancellationToken ct = default)
    {
        // İdempotency: aynı anahtarla satış varsa onu döndür
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _db.Sales.AsNoTracking()
                .Where(s => s.IdempotencyKey == request.IdempotencyKey)
                .Select(s => s.Id).FirstOrDefaultAsync(ct);
            if (existing != Guid.Empty)
                return await GetByIdAsync(existing, ct);
        }

        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            return Result.Failure<SaleDto>(Error.Validation("Geçersiz müşteri."));
        if (!await _db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, ct))
            return Result.Failure<SaleDto>(Error.Validation("Geçersiz depo."));

        var variantIds = request.Lines.Select(l => l.VariantId).Distinct().ToList();
        var variants = await _db.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);
        if (variants.Count != variantIds.Count)
            return Result.Failure<SaleDto>(Error.Validation("Bir veya daha fazla varyant bulunamadı."));

        var sale = new Sale
        {
            SaleNumber = await _sequence.NextAsync(SequenceNames.SaleNumber, ct),
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            SaleDate = request.SaleDate ?? DateTime.UtcNow,
            Status = SaleStatus.Active,
            IdempotencyKey = request.IdempotencyKey,
            Note = request.Note
        };

        decimal subTotal = 0, lineDiscountTotal = 0, costTotal = 0;

        foreach (var l in request.Lines)
        {
            var variant = variants[l.VariantId];
            var adetQty = l.UnitType == UnitType.Koli ? l.Quantity * variant.KoliIciAdet : l.Quantity;

            var unitPrice = l.UnitPrice ?? (l.UnitType == UnitType.Koli
                ? variant.SalePrice * variant.KoliIciAdet
                : variant.SalePrice);

            var lineGross = unitPrice * l.Quantity;
            var lineTotal = lineGross - l.LineDiscount;
            var unitCost = variant.AverageCost;

            subTotal += lineGross;
            lineDiscountTotal += l.LineDiscount;
            costTotal += unitCost * adetQty;

            sale.Lines.Add(new SaleLine
            {
                VariantId = variant.Id,
                UnitType = l.UnitType,
                Quantity = l.Quantity,
                AdetQuantity = adetQty,
                UnitPrice = unitPrice,
                UnitCost = unitCost,
                LineDiscount = l.LineDiscount,
                LineTotal = lineTotal
            });

            await _stock.ApplyAsync(new StockMovementInput(
                StockMovementType.Satis, variant.Id, request.WarehouseId, -adetQty,
                ReferenceType: "Sale", ReferenceId: sale.Id, Note: request.Note), ct);
        }

        sale.SubTotal = subTotal;
        sale.DiscountTotal = lineDiscountTotal + request.DocumentDiscount;
        sale.GrandTotal = subTotal - sale.DiscountTotal;
        sale.CostTotal = costTotal;

        if (sale.GrandTotal < 0)
            return Result.Failure<SaleDto>(Error.Validation("İskonto toplam tutarı aşamaz."));

        _db.Sales.Add(sale);

        // Cari: satış borcu
        await _account.PostAsync(new AccountTransactionInput(
            AccountTransactionType.Satis, request.CustomerId, AccountDirection.Borc, sale.GrandTotal,
            ReferenceType: "Sale", ReferenceId: sale.Id, Note: $"Satış #{sale.SaleNumber}"), ct);

        // Opsiyonel peşin/kısmi ödeme
        if (request.InitialPayment is { Amount: > 0 } pay)
        {
            _db.Payments.Add(new Payment
            {
                CustomerId = request.CustomerId,
                SaleId = sale.Id,
                Type = pay.Type,
                Amount = pay.Amount,
                PaymentDate = sale.SaleDate,
                DueDate = pay.DueDate
            });
            sale.PaidAmount = pay.Amount;

            await _account.PostAsync(new AccountTransactionInput(
                AccountTransactionType.Tahsilat, request.CustomerId, AccountDirection.Alacak, pay.Amount,
                ReferenceType: "Sale", ReferenceId: sale.Id, Note: $"Satış #{sale.SaleNumber} tahsilat"), ct);
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct);
    }

    public async Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _db.Sales.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Lines).ThenInclude(l => l.Variant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sale is null)
            return Result.Failure<SaleDto>(Error.NotFound("Satış bulunamadı."));

        return Result.Success(MapSale(sale));
    }

    public async Task<PagedResult<SaleListItemDto>> GetAllAsync(SaleQuery query, CancellationToken ct = default)
    {
        var q = _db.Sales.AsNoTracking().AsQueryable();

        if (query.CustomerId is { } cid) q = q.Where(s => s.CustomerId == cid);
        if (query.Status is { } st) q = q.Where(s => s.Status == st);
        if (query.FromDate is { } f) q = q.Where(s => s.SaleDate >= f);
        if (query.ToDate is { } t) q = q.Where(s => s.SaleDate <= t);

        q = q.OrderByDescending(s => s.SaleNumber);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SaleListItemDto(s.Id, s.SaleNumber, s.Customer.Name, s.SaleDate, s.Status, s.GrandTotal, s.PaidAmount))
            .ToListAsync(ct);

        return new PagedResult<SaleListItemDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Result> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _db.Sales.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sale is null)
            return Result.Failure(Error.NotFound("Satış bulunamadı."));
        if (sale.Status == SaleStatus.Cancelled)
            return Result.Success(); // idempotent

        // Daha önce iade edilmiş adetler ve tutar (çifte sayımı önlemek için yalnızca kalanı tersle)
        var returnedQty = await _db.SaleReturnLines.AsNoTracking()
            .Where(rl => rl.SaleReturn.SaleId == sale.Id)
            .GroupBy(rl => rl.SaleLineId)
            .Select(g => new { g.Key, Total = g.Sum(x => x.AdetQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);

        var returnedAmount = await _db.SaleReturns.AsNoTracking()
            .Where(r => r.SaleId == sale.Id)
            .SumAsync(r => (decimal?)r.TotalAmount, ct) ?? 0m;

        // Stok: yalnızca henüz iade edilmemiş adetleri tersle (storno)
        foreach (var line in sale.Lines)
        {
            var remaining = line.AdetQuantity - returnedQty.GetValueOrDefault(line.Id, 0);
            if (remaining <= 0)
                continue;

            await _stock.ApplyAsync(new StockMovementInput(
                StockMovementType.SatisIptali, line.VariantId, sale.WarehouseId, remaining,
                ReferenceType: "SaleCancel", ReferenceId: sale.Id, Note: $"Satış #{sale.SaleNumber} iptali"), ct);
        }

        // Cari: satış borcunun iade ile kapatılmamış kısmını geri al (alacak)
        var reversibleAmount = sale.GrandTotal - returnedAmount;
        if (reversibleAmount > 0)
        {
            await _account.PostAsync(new AccountTransactionInput(
                AccountTransactionType.Iade, sale.CustomerId, AccountDirection.Alacak, reversibleAmount,
                ReferenceType: "SaleCancel", ReferenceId: sale.Id, Note: $"Satış #{sale.SaleNumber} iptali"), ct);
        }

        sale.Status = SaleStatus.Cancelled;
        sale.CancelledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<SaleDto>> ReturnAsync(ReturnSaleRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var done = await _db.SaleReturns.AnyAsync(r => r.IdempotencyKey == request.IdempotencyKey, ct);
            if (done)
                return await GetByIdAsync(request.SaleId, ct);
        }

        var sale = await _db.Sales.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == request.SaleId, ct);
        if (sale is null)
            return Result.Failure<SaleDto>(Error.NotFound("Satış bulunamadı."));
        if (sale.Status == SaleStatus.Cancelled)
            return Result.Failure<SaleDto>(Error.Conflict("İptal edilmiş satış için iade yapılamaz."));

        // Her satır için daha önce iade edilen adetler
        var alreadyReturned = await _db.SaleReturnLines.AsNoTracking()
            .Where(rl => rl.SaleReturn.SaleId == sale.Id)
            .GroupBy(rl => rl.SaleLineId)
            .Select(g => new { SaleLineId = g.Key, Total = g.Sum(x => x.AdetQuantity) })
            .ToDictionaryAsync(x => x.SaleLineId, x => x.Total, ct);

        var ret = new SaleReturn
        {
            SaleId = sale.Id,
            CustomerId = sale.CustomerId,
            WarehouseId = sale.WarehouseId,
            ReturnDate = DateTime.UtcNow,
            IdempotencyKey = request.IdempotencyKey,
            Note = request.Note
        };

        decimal totalRefund = 0;

        foreach (var rl in request.Lines)
        {
            var saleLine = sale.Lines.FirstOrDefault(l => l.Id == rl.SaleLineId);
            if (saleLine is null)
                return Result.Failure<SaleDto>(Error.Validation("İade satırı bu satışa ait değil."));

            var prev = alreadyReturned.GetValueOrDefault(saleLine.Id, 0);
            var remaining = saleLine.AdetQuantity - prev;
            if (rl.AdetQuantity > remaining)
                return Result.Failure<SaleDto>(Error.Validation($"İade miktarı kalan iade edilebilir adedi ({remaining}) aşıyor."));

            // Satırın iskonto dahil adet başı net fiyatı
            var perAdetNet = saleLine.AdetQuantity > 0 ? saleLine.LineTotal / saleLine.AdetQuantity : 0m;
            var lineRefund = perAdetNet * rl.AdetQuantity;
            totalRefund += lineRefund;

            ret.Lines.Add(new SaleReturnLine
            {
                SaleLineId = saleLine.Id,
                VariantId = saleLine.VariantId,
                AdetQuantity = rl.AdetQuantity,
                UnitPrice = perAdetNet,
                UnitCost = saleLine.UnitCost,
                LineTotal = lineRefund
            });

            await _stock.ApplyAsync(new StockMovementInput(
                StockMovementType.Iade, saleLine.VariantId, sale.WarehouseId, rl.AdetQuantity,
                ReferenceType: "SaleReturn", ReferenceId: ret.Id, Note: request.Note), ct);
        }

        ret.TotalAmount = totalRefund;
        _db.SaleReturns.Add(ret);

        await _account.PostAsync(new AccountTransactionInput(
            AccountTransactionType.Iade, sale.CustomerId, AccountDirection.Alacak, totalRefund,
            ReferenceType: "SaleReturn", ReferenceId: ret.Id, Note: $"Satış #{sale.SaleNumber} iade"), ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct);
    }

    private static SaleDto MapSale(Sale s) => new(
        s.Id, s.SaleNumber, s.CustomerId, s.Customer.Name, s.WarehouseId,
        s.SaleDate, s.Status, s.SubTotal, s.DiscountTotal, s.GrandTotal, s.CostTotal,
        s.PaidAmount, s.GrandTotal - s.CostTotal, s.Note, s.CancelledAt,
        s.Lines.Select(l => new SaleLineDto(
            l.Id, l.VariantId, l.Variant.Product.Name, l.Variant.Color, l.Variant.Size,
            l.UnitType, l.Quantity, l.AdetQuantity,
            l.UnitPrice, l.UnitCost, l.LineDiscount, l.LineTotal)).ToList());
}
