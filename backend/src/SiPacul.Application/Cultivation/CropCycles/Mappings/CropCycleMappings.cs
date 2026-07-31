using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.CropCycles.Mappings;

public static class CropCycleMappings
{
    public static CropCycleResponse ToResponse(
        this CropCycle cropCycle)
    {
        ArgumentNullException.ThrowIfNull(cropCycle);

        return new CropCycleResponse(
            cropCycle.Id,
            cropCycle.OrganizationId,
            cropCycle.Code,
            cropCycle.Name,
            cropCycle.CommodityId,
            cropCycle.CultivationSopId,
            cropCycle.LandId,
            cropCycle.LandPlotId,
            cropCycle.PlantedArea,
            cropCycle.AreaUnit,
            cropCycle.PlantedAreaInSquareMeters,
            cropCycle.PlannedStartDate,
            cropCycle.ExpectedHarvestDate,
            cropCycle.ActualStartDate,
            cropCycle.ActualHarvestDate,
            cropCycle.Status,
            cropCycle.CancellationReason,
            cropCycle.Notes,
            cropCycle.CreatedAt,
            cropCycle.UpdatedAt);
    }
}
