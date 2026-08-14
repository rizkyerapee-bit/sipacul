using SiPacul.Application.Finance.ProfitSharing.Assignments.Mappings;
using SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;
using SiPacul.Application.Finance.Profitability.Mappings;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Finance.ProfitSharing.Previews.Mappings;

public static class ProfitSharingPreviewMappings
{
    public static ProfitSharingPreviewResponse ToPreviewResponse(
        this ProfitSharingWaterfallCalculationResult result,
        ProfitSharingSchemeAssignment assignment,
        CropCycleProfitabilityReport profitability,
        HarvestQuantityUnit? harvestQuantityUnit)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(profitability);

        return new ProfitSharingPreviewResponse(
            result.OrganizationId,
            result.CropCycleId,
            false,
            result.CalculationVersion,
            profitability.GeneratedAt,
            assignment.ToResponse(),
            profitability.ToResponse(harvestQuantityUnit),
            new ProfitSharingPreviewTotalsResponse(
                result.TotalCapital,
                result.TotalCapitalRecovery,
                result.TotalCapitalLoss,
                result.TotalManagementProfitShare,
                result.TotalReturnOnCapitalProfitShare,
                result.TotalPriorityProfitShare,
                result.TotalResidualProfitShare,
                result.TotalProfitShare,
                result.TotalPayout,
                result.ResidualMethod),
            result.PriorityAllocations
                .OrderBy(allocation => allocation.Sequence)
                .Select(allocation =>
                    new ProfitSharingPriorityAllocationPreviewResponse(
                        allocation.RuleCode,
                        allocation.RuleType,
                        allocation.RecipientCodeSnapshot,
                        allocation.RecipientNameSnapshot,
                        allocation.Rate.Numerator,
                        allocation.Rate.Denominator,
                        allocation.BaseAmount,
                        allocation.RequestedAmount,
                        allocation.AllocatedAmount,
                        allocation.UnallocatedAmount,
                        allocation.Sequence))
                .ToArray(),
            result.Allocations
                .OrderBy(allocation => allocation.Sequence)
                .Select(allocation =>
                    new ProfitSharingParticipantAllocationPreviewResponse(
                        allocation.ParticipantCodeSnapshot,
                        allocation.ParticipantNameSnapshot,
                        allocation.ParticipantRole,
                        allocation.ConfirmedCapital,
                        allocation.CapitalRatio,
                        allocation.ParticipatesInResidualProfit,
                        allocation.CapitalRecovery,
                        allocation.CapitalLoss,
                        allocation.ManagementProfitShare,
                        allocation.ReturnOnCapitalProfitShare,
                        allocation.ResidualProfitShare,
                        allocation.TotalProfitShare,
                        allocation.TotalPayout,
                        allocation.Sequence))
                .ToArray());
    }
}
