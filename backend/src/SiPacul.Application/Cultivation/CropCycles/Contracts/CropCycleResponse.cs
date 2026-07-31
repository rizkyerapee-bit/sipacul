using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Cultivation.CropCycles.Contracts;

public sealed record CropCycleResponse(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    Guid CommodityId,
    Guid? CultivationSopId,
    Guid LandId,
    Guid LandPlotId,
    decimal PlantedArea,
    AreaUnit AreaUnit,
    decimal PlantedAreaInSquareMeters,
    DateOnly PlannedStartDate,
    DateOnly ExpectedHarvestDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualHarvestDate,
    CropCycleStatus Status,
    string? CancellationReason,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
