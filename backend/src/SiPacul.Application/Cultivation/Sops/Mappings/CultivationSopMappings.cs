using SiPacul.Application.Cultivation.Sops.Contracts;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Sops.Mappings;

internal static class CultivationSopMappings
{
    public static CultivationSopResponse ToResponse(
        this CultivationSop sop)
    {
        IReadOnlyList<CultivationSopStepResponse> steps =
            sop.Steps
                .OrderBy(step => step.Sequence)
                .Select(step => step.ToResponse())
                .ToArray();

        return new CultivationSopResponse(
            sop.Id,
            sop.OrganizationId,
            sop.CommodityId,
            sop.Name,
            sop.Description,
            sop.IsActive,
            sop.CreatedAt,
            sop.UpdatedAt,
            steps);
    }

    private static CultivationSopStepResponse ToResponse(
        this CultivationSopStep step)
    {
        return new CultivationSopStepResponse(
            step.Id,
            step.OrganizationId,
            step.CultivationSopId,
            step.Sequence,
            step.Name,
            step.Description,
            step.PlannedDayOffset,
            step.EstimatedDurationDays,
            step.IsRequired,
            step.CreatedAt,
            step.UpdatedAt);
    }
}
