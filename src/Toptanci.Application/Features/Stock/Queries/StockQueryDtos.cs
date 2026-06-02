using Toptanci.Application.Common;
using Toptanci.Domain.Enums;

namespace Toptanci.Application.Features.Stock.Queries;

public sealed record StockLevelDto(
    Guid VariantId,
    string ProductName,
    string? Color,
    string? Pattern,
    string? Size,
    string AdetBarcode,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int MinStock,
    bool IsBelowMin);

public sealed record StockMovementDto(
    Guid Id,
    StockMovementType Type,
    Guid VariantId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    decimal? UnitCost,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed record MovementQuery : PageQuery
{
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public StockMovementType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
