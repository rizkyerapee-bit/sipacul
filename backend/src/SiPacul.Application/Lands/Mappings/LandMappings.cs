using SiPacul.Application.Lands.Contracts;
using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Lands.Mappings;

internal static class LandMappings
{
    public static LandResponse ToResponse(
        this Land land)
    {
        IReadOnlyList<LandPlotResponse> plots =
            land.Plots
                .OrderBy(plot => plot.Code)
                .Select(plot => plot.ToResponse())
                .ToArray();

        return new LandResponse(
            land.Id,
            land.OrganizationId,
            land.Code,
            land.Name,
            land.TenureType,
            land.TotalArea,
            land.AreaUnit,
            land.TotalAreaInSquareMeters,
            land.AllocatedPlotAreaInSquareMeters,
            land.Address,
            land.LocationDescription,
            land.Latitude,
            land.Longitude,
            land.Notes,
            land.IsActive,
            land.CreatedAt,
            land.UpdatedAt,
            plots);
    }

    private static LandPlotResponse ToResponse(
        this LandPlot plot)
    {
        return new LandPlotResponse(
            plot.Id,
            plot.LandId,
            plot.Code,
            plot.Name,
            plot.Area,
            plot.AreaUnit,
            plot.GeneralCondition,
            plot.Notes,
            plot.IsActive,
            plot.CreatedAt,
            plot.UpdatedAt);
    }
}
