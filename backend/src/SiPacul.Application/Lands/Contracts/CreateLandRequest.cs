using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Lands.Contracts;

public sealed record CreateLandRequest(
    string Code,
    string Name,
    LandTenureType TenureType,
    decimal TotalArea,
    AreaUnit AreaUnit,
    string? Address,
    string? LocationDescription,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes);
