using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Persistence;

public interface ICultivationActivityRepository
{
    Task<IReadOnlyList<CultivationActivity>> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        CultivationActivityStatus? status = null,
        CultivationActivityType? activityType = null,
        DateOnly? plannedFrom = null,
        DateOnly? plannedTo = null,
        Guid? cultivationSopStepId = null,
        CancellationToken cancellationToken = default);

    Task<CultivationActivity?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancellationToken cancellationToken = default);

    Task<CultivationActivity?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> HasInProgressActivitiesAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyActivityForSopStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid cultivationSopStepId,
        CancellationToken cancellationToken = default);

    void Add(CultivationActivity activity);
}
