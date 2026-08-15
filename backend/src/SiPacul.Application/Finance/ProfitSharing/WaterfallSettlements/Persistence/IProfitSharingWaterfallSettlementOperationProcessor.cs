namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;

public interface IProfitSharingWaterfallSettlementOperationProcessor
{
    Task<ProfitSharingWaterfallSettlementOperationResult> FinalizeAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        DateOnly settlementDate,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingWaterfallSettlementOperationResult> VoidAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        string voidReason,
        CancellationToken cancellationToken = default);
}
