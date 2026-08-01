using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Sales.Persistence;

public sealed record SaleHarvestReference(
    Guid HarvestBatchId,
    string HarvestBatchCode,
    Guid CropCycleId,
    string CropCycleCode,
    Guid CommodityId,
    string CommodityCode,
    string CommodityName,
    string? QualityGrade,
    HarvestBatchStatus Status,
    decimal NetQuantity,
    HarvestQuantityUnit QuantityUnit);
