using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Lands.Contracts;

public sealed record AddLandPlotRequest(
    string Code,
    string Name,
    decimal Area,
    AreaUnit AreaUnit,
    string? GeneralCondition,
    string? Notes);
