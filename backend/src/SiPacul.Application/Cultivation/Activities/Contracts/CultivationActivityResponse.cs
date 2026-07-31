using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record CultivationActivityResourceResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CultivationActivityId,
    CultivationResourceType ResourceType,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    decimal TotalCost,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CultivationActivityResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    string Code,
    string Name,
    CultivationActivityType ActivityType,
    Guid? CultivationSopId,
    Guid? CultivationSopStepId,
    int? SopStepSequenceSnapshot,
    string? SopStepNameSnapshot,
    int? SopPlannedDayOffsetSnapshot,
    int? SopEstimatedDurationDaysSnapshot,
    bool? SopIsRequiredSnapshot,
    DateOnly PlannedDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualCompletionDate,
    CultivationActivityStatus Status,
    SopComplianceStatus SopComplianceStatus,
    string? Outcome,
    string? IssueNotes,
    string? DeviationReason,
    string? CancellationReason,
    string? Notes,
    decimal TotalActualCost,
    IReadOnlyList<CultivationActivityResourceResponse> Resources,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
