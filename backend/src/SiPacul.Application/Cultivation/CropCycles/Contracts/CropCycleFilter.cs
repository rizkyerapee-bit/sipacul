using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.CropCycles.Contracts;

public sealed record CropCycleFilter(
    CropCycleStatus? Status = null,
    Guid? CommodityId = null,
    Guid? LandId = null,
    Guid? LandPlotId = null,
    DateOnly? PlannedStartFrom = null,
    DateOnly? PlannedStartTo = null);
