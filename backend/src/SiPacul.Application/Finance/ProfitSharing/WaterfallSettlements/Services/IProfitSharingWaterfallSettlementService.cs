using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Services;

public interface IProfitSharingWaterfallSettlementService
{
    Task<Result<ProfitSharingWaterfallSettlementResponse>> FinalizeAsync(
        Guid organizationId,
        Guid cropCycleId,
        FinalizeProfitSharingWaterfallSettlementRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingWaterfallSettlementFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingWaterfallSettlementResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingWaterfallSettlementResponse>> VoidAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        VoidProfitSharingWaterfallSettlementRequest request,
        CancellationToken cancellationToken = default);
}
