namespace SiPacul.Application.Evaluations.SeasonHistories.Persistence;

public sealed record SeasonHistoryPageSource(
    int TotalCount,
    IReadOnlyList<SeasonHistoryCycleSource> Cycles);
