namespace SiPacul.Application.Evaluations.SeasonHistories.Contracts;

public sealed record SeasonHistoryFilter(
    Guid? LandPlotId = null,
    bool IncludeNonTerminal = false,
    int Page = 1,
    int PageSize = 20);
