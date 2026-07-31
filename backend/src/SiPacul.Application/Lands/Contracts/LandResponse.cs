using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Lands.Contracts;

public sealed record LandResponse(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    LandTenureType TenureType,
    decimal TotalArea,
    AreaUnit AreaUnit,
    decimal TotalAreaInSquareMeters,
    decimal AllocatedPlotAreaInSquareMeters,
    string? Address,
    string? LocationDescription,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<LandPlotResponse> Plots);
