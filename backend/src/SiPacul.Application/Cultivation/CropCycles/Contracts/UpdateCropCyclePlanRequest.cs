using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Cultivation.CropCycles.Contracts;

public sealed record UpdateCropCyclePlanRequest(
    string Name,
    Guid? CultivationSopId,
    decimal PlantedArea,
    AreaUnit AreaUnit,
    DateOnly PlannedStartDate,
    DateOnly ExpectedHarvestDate,
    string? Notes);
