using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Sales.Contracts;

public sealed record SaleLineResponse(
    Guid Id,
    Guid HarvestBatchId,
    string HarvestBatchCodeSnapshot,
    Guid CropCycleIdSnapshot,
    string CropCycleCodeSnapshot,
    Guid CommodityIdSnapshot,
    string CommodityCodeSnapshot,
    string CommodityNameSnapshot,
    string? QualityGradeSnapshot,
    decimal Quantity,
    HarvestQuantityUnit QuantityUnit,
    decimal UnitPrice,
    decimal LineDiscount,
    decimal LineTotal,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
