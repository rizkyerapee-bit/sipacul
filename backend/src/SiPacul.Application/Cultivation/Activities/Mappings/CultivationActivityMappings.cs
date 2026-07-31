using SiPacul.Application.Cultivation.Activities.Contracts;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Mappings;

public static class CultivationActivityMappings
{
    public static CultivationActivityResponse ToResponse(
        this CultivationActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        IReadOnlyList<CultivationActivityResourceResponse>
            resources = activity.Resources
                .OrderBy(resource => resource.CreatedAt)
                .ThenBy(resource => resource.Id)
                .Select(resource =>
                    resource.ToResponse())
                .ToArray();

        return new CultivationActivityResponse(
            activity.Id,
            activity.OrganizationId,
            activity.CropCycleId,
            activity.Code,
            activity.Name,
            activity.ActivityType,
            activity.CultivationSopId,
            activity.CultivationSopStepId,
            activity.SopStepSequenceSnapshot,
            activity.SopStepNameSnapshot,
            activity.SopPlannedDayOffsetSnapshot,
            activity.SopEstimatedDurationDaysSnapshot,
            activity.SopIsRequiredSnapshot,
            activity.PlannedDate,
            activity.ActualStartDate,
            activity.ActualCompletionDate,
            activity.Status,
            activity.SopComplianceStatus,
            activity.Outcome,
            activity.IssueNotes,
            activity.DeviationReason,
            activity.CancellationReason,
            activity.Notes,
            activity.TotalActualCost,
            resources,
            activity.CreatedAt,
            activity.UpdatedAt);
    }

    public static CultivationActivityResourceResponse
        ToResponse(
            this CultivationActivityResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new CultivationActivityResourceResponse(
            resource.Id,
            resource.OrganizationId,
            resource.CultivationActivityId,
            resource.ResourceType,
            resource.Description,
            resource.Quantity,
            resource.Unit,
            resource.UnitCost,
            resource.TotalCost,
            resource.Notes,
            resource.CreatedAt,
            resource.UpdatedAt);
    }
}
