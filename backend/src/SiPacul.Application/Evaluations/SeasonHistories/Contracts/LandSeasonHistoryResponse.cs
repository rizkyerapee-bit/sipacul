namespace SiPacul.Application.Evaluations.SeasonHistories.Contracts;

public sealed record LandSeasonHistoryResponse(
    Guid OrganizationId,
    Guid LandId,
    string LandCode,
    string LandName,
    Guid? LandPlotId,
    string? LandPlotCode,
    string? LandPlotName,
    bool IncludeNonTerminal,
    int Page,
    int PageSize,
    int TotalSeasonCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    IReadOnlyList<SeasonEvaluationResponse> Seasons,
    DateTime GeneratedAt);
