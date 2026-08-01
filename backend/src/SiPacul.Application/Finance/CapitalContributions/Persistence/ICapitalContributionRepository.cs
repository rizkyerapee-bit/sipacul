using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.CapitalContributions.Persistence;

public interface ICapitalContributionRepository
{
    Task<IReadOnlyList<CapitalContribution>> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        CapitalContributionStatus? status = null,
        CapitalContributorRole? contributorRole = null,
        DateOnly? contributionDateFrom = null,
        DateOnly? contributionDateTo = null,
        string? contributorCode = null,
        string? contributorName = null,
        CancellationToken cancellationToken = default);

    Task<CapitalContribution?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancellationToken cancellationToken = default);

    Task<CapitalContribution?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default);

    void Add(CapitalContribution contribution);
}
