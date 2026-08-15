using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Mappings;

public static class ProfitSharingWaterfallSettlementMappings
{
    public static ProfitSharingWaterfallSettlementResponse ToResponse(
        this ProfitSharingWaterfallSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        return new ProfitSharingWaterfallSettlementResponse(
            settlement.Id,
            settlement.OrganizationId,
            settlement.CropCycleId,
            settlement.AssignmentId,
            settlement.SourceSchemeId,
            settlement.SchemeFamilyId,
            settlement.Code,
            settlement.SettlementDate,
            settlement.SchemeCodeSnapshot,
            settlement.SchemeNameSnapshot,
            settlement.SchemeDescriptionSnapshot,
            settlement.SchemeVersionSnapshot,
            settlement.SchemeAssignedAtSnapshot,
            settlement.ResidualMethod,
            settlement.ResidualRecipientCodeSnapshot,
            settlement.CropCycleCodeSnapshot,
            settlement.CropCycleNameSnapshot,
            settlement.CommodityIdSnapshot,
            settlement.CommodityCodeSnapshot,
            settlement.CommodityNameSnapshot,
            settlement.RecognizedRevenue,
            settlement.CollectedRevenue,
            settlement.OutstandingReceivable,
            settlement.ActivityResourceCost,
            settlement.ManualExpenseCost,
            settlement.TotalCultivationCost,
            settlement.NetProfit,
            settlement.Outcome,
            settlement.ConfirmedInvestorCapital,
            settlement.ConfirmedPartnerCapital,
            settlement.TotalConfirmedCapital,
            settlement.AvailableHarvestQuantity,
            settlement.TotalCapital,
            settlement.TotalCapitalRecovery,
            settlement.TotalCapitalLoss,
            settlement.TotalManagementProfitShare,
            settlement.TotalReturnOnCapitalProfitShare,
            settlement.TotalPriorityProfitShare,
            settlement.TotalResidualProfitShare,
            settlement.TotalProfitShare,
            settlement.TotalPayout,
            settlement.CalculationVersion,
            settlement.CalculatedAt,
            settlement.Notes,
            settlement.Status,
            settlement.FinalizedAt,
            settlement.VoidedAt,
            settlement.VoidReason,
            settlement.CreatedAt,
            settlement.UpdatedAt,
            settlement.PriorityAllocations
                .OrderBy(allocation => allocation.Sequence)
                .Select(allocation =>
                    new ProfitSharingWaterfallPriorityAllocationResponse(
                        allocation.Id,
                        allocation.RuleCode,
                        allocation.RuleType,
                        allocation.RecipientCodeSnapshot,
                        allocation.RecipientNameSnapshot,
                        allocation.RateNumerator,
                        allocation.RateDenominator,
                        allocation.BaseAmount,
                        allocation.RequestedAmount,
                        allocation.AllocatedAmount,
                        allocation.UnallocatedAmount,
                        allocation.Sequence))
                .ToArray(),
            settlement.ParticipantAllocations
                .OrderBy(allocation => allocation.Sequence)
                .Select(allocation =>
                    new ProfitSharingWaterfallParticipantAllocationResponse(
                        allocation.Id,
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
                .ToArray(),
            settlement.ResidualShares
                .OrderBy(share => share.Sequence)
                .Select(share =>
                    new ProfitSharingWaterfallResidualShareResponse(
                        share.Id,
                        share.RecipientCodeSnapshot,
                        share.RateNumerator,
                        share.RateDenominator,
                        share.Sequence))
                .ToArray());
    }
}
