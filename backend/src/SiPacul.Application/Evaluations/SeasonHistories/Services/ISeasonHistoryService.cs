using SiPacul.Application.Evaluations.SeasonHistories.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Evaluations.SeasonHistories.Services;

public interface ISeasonHistoryService
{
    Task<Result<LandSeasonHistoryResponse>> GetAsync(
        Guid organizationId,
        Guid landId,
        SeasonHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);
}
