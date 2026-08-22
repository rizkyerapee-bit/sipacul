namespace SiPacul.Application.Evaluations.SeasonHistories.Persistence;

public interface ISeasonHistoryReadRepository
{
    Task<SeasonHistoryPageSource> GetPageAsync(
        Guid organizationId,
        Guid landId,
        Guid? landPlotId,
        bool includeNonTerminal,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
