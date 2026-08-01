using SiPacul.Application.Finance.CapitalContributions.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.CapitalContributions.Services;

public interface ICapitalContributionService
{
    Task<Result<CapitalContributionResponse>> CreateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CreateCapitalContributionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CapitalContributionResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CapitalContributionFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<CapitalContributionResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancellationToken cancellationToken = default);

    Task<Result<CapitalContributionResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        UpdateCapitalContributionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CapitalContributionResponse>> ConfirmAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancellationToken cancellationToken = default);

    Task<Result<CapitalContributionResponse>> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancelCapitalContributionRequest request,
        CancellationToken cancellationToken = default);
}
