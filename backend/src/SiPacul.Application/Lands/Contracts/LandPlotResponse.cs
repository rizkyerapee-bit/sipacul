using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Lands.Contracts;

public sealed record LandPlotResponse(
    Guid Id,
    Guid LandId,
    string Code,
    string Name,
    decimal Area,
    AreaUnit AreaUnit,
    string? GeneralCondition,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
