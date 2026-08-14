using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;

public interface IProfitSharingWaterfallSettlementRepository
{
    Task<IReadOnlyList<ProfitSharingWaterfallSettlement>> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        ProfitSharingWaterfallSettlementStatus? status = null,
        DateOnly? settlementDateFrom = null,
        DateOnly? settlementDateTo = null,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingWaterfallSettlement?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingWaterfallSettlement?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingWaterfallSettlement?> GetActiveFinalizedAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default);

    void Add(ProfitSharingWaterfallSettlement settlement);
}
