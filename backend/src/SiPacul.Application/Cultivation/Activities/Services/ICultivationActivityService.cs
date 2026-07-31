using SiPacul.Application.Cultivation.Activities.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.Activities.Services;

public interface ICultivationActivityService
{
    Task<Result<CultivationActivityResponse>> CreateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CreateCultivationActivityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CultivationActivityResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CultivationActivityFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>>
        UpdatePlanAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            UpdateCultivationActivityPlanRequest request,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>> StartAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        StartCultivationActivityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>> CompleteAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CompleteCultivationActivityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancelCultivationActivityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>>
        UpdateExecutionNotesAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            UpdateCultivationActivityNotesRequest request,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>>
        AddResourceAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            AddCultivationActivityResourceRequest request,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>>
        UpdateResourceAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            Guid resourceId,
            UpdateCultivationActivityResourceRequest request,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationActivityResponse>>
        RemoveResourceAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            Guid resourceId,
            CancellationToken cancellationToken = default);
}
