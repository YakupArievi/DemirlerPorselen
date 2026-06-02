using Microsoft.EntityFrameworkCore;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Domain.Entities;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Stock;

/// <summary>Bir stok hareketi için girdi (adet bazlı, işaretli miktar).</summary>
public sealed record StockMovementInput(
    StockMovementType Type,
    Guid VariantId,
    Guid WarehouseId,
    int Quantity,
    decimal? UnitCost = null,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? IdempotencyKey = null,
    string? Note = null);

/// <summary>
/// Stok ledger çekirdeği (mimari kural 6). StockMovement ekler ve StockItem cache'ini
/// change-tracker'da günceller. SaveChanges ÇAĞIRMAZ — çağıran tek transaction içinde toplar.
/// </summary>
public interface IStockLedger
{
    /// <summary>
    /// Hareketi uygular. IdempotencyKey daha önce işlenmişse hiçbir şey yapmaz ve null döner.
    /// </summary>
    Task<StockMovement?> ApplyAsync(StockMovementInput input, CancellationToken ct = default);
}

public sealed class StockLedger : IStockLedger
{
    private readonly IApplicationDbContext _db;

    public StockLedger(IApplicationDbContext db) => _db = db;

    public async Task<StockMovement?> ApplyAsync(StockMovementInput input, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(input.IdempotencyKey))
        {
            var already = await _db.StockMovements
                .AnyAsync(m => m.IdempotencyKey == input.IdempotencyKey, ct);
            if (already)
                return null;
        }

        var stockItem = await _db.StockItems
            .FirstOrDefaultAsync(s => s.VariantId == input.VariantId && s.WarehouseId == input.WarehouseId, ct);

        if (stockItem is null)
        {
            stockItem = new StockItem
            {
                VariantId = input.VariantId,
                WarehouseId = input.WarehouseId,
                Quantity = 0
            };
            _db.StockItems.Add(stockItem);
        }

        // Eksi stoğa izin verilir (mimari kural: eksi stok serbest)
        stockItem.Quantity += input.Quantity;

        var movement = new StockMovement
        {
            Type = input.Type,
            VariantId = input.VariantId,
            WarehouseId = input.WarehouseId,
            Quantity = input.Quantity,
            UnitCost = input.UnitCost,
            ReferenceType = input.ReferenceType,
            ReferenceId = input.ReferenceId,
            IdempotencyKey = input.IdempotencyKey,
            Note = input.Note
        };
        _db.StockMovements.Add(movement);

        return movement;
    }
}
