using SiPacul.Domain.Entities.Finance.ProfitSharing;

namespace SiPacul.Application.Finance.ProfitSharing.Persistence;

public interface IProfitSharingSettlementRepository
{
    Task<IReadOnlyList<ProfitSharingSettlement>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingSettlementStatus? status = null,
            DateOnly? settlementDateFrom = null,
            DateOnly? settlementDateTo = null,
            string? managingPartnerCode = null,
            CancellationToken cancellationToken = default);

    Task<ProfitSharingSettlement?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingSettlement?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingSettlement?>
        GetActiveFinalizedAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default);

    Task<ProfitSharingSettlement?>
        GetActiveFinalizedForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveFinalizedAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid? excludedSettlementId = null,
        CancellationToken cancellationToken = default);

    void Add(ProfitSharingSettlement settlement);
}
