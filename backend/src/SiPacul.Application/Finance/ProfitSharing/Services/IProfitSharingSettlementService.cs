using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Services;

public interface IProfitSharingSettlementService
{
    Task<Result<ProfitSharingSettlementResponse>>
        CreateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            CreateProfitSharingSettlementRequest request,
            CancellationToken cancellationToken = default);

    Task<
        Result<
            IReadOnlyList<
                ProfitSharingSettlementResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingSettlementFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSettlementResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSettlementResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            UpdateProfitSharingSettlementRequest request,
            CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSettlementResponse>>
        VoidAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            VoidProfitSharingSettlementRequest request,
            CancellationToken cancellationToken = default);
}
