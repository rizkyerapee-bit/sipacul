using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Cultivation.CropCycles.Contracts;

public sealed record CreateCropCycleRequest(
    string Code,
    string Name,
    Guid CommodityId,
    Guid? CultivationSopId,
    Guid LandId,
    Guid LandPlotId,
    decimal PlantedArea,
    AreaUnit AreaUnit,
    DateOnly PlannedStartDate,
    DateOnly ExpectedHarvestDate,
    string? Notes);
