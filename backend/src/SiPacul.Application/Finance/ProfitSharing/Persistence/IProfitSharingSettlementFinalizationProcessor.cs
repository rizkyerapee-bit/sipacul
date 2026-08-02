namespace SiPacul.Application.Finance.ProfitSharing.Persistence;

public interface
    IProfitSharingSettlementFinalizationProcessor
{
    Task<ProfitSharingFinalizationResult> FinalizeAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default);
}
