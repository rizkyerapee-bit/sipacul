using SiPacul.Application.Harvests.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Harvests.Services;

public interface IHarvestBatchService
{
    Task<Result<HarvestBatchResponse>> CreateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CreateHarvestBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<HarvestBatchResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            HarvestBatchFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<HarvestBatchResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    Task<Result<HarvestBatchResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        UpdateHarvestBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<HarvestBatchResponse>> ConfirmAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    Task<Result<HarvestBatchResponse>> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancelHarvestBatchRequest request,
        CancellationToken cancellationToken = default);
}
