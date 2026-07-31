using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.CropCycles.Persistence;

public interface ICropCycleRepository
{
    Task<IReadOnlyList<CropCycle>> GetAllAsync(
        Guid organizationId,
        CropCycleStatus? status = null,
        Guid? commodityId = null,
        Guid? landId = null,
        Guid? landPlotId = null,
        DateOnly? plannedStartFrom = null,
        DateOnly? plannedStartTo = null,
        CancellationToken cancellationToken = default);

    Task<CropCycle?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<CropCycle?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> HasScheduleConflictAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        DateOnly plannedStartDate,
        DateOnly expectedHarvestDate,
        Guid? excludedCropCycleId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasInProgressCycleAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        Guid? excludedCropCycleId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveCycleForLandAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveCycleForPlotAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyCycleForPlotAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        CancellationToken cancellationToken = default);

    void Add(CropCycle cropCycle);
}
