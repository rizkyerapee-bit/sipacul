using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Harvests.Persistence;

public interface IHarvestBatchRepository
{
    Task<IReadOnlyList<HarvestBatch>> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        HarvestBatchStatus? status = null,
        DateOnly? harvestDateFrom = null,
        DateOnly? harvestDateTo = null,
        HarvestQuantityUnit? quantityUnit = null,
        string? qualityGrade = null,
        CancellationToken cancellationToken = default);

    Task<HarvestBatch?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    Task<HarvestBatch?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> HasDraftBatchesAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasNonCancelledBatchesAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    void Add(HarvestBatch harvestBatch);
}
