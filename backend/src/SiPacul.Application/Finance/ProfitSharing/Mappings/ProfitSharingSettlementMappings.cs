using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Domain.Entities.Finance.ProfitSharing;

namespace SiPacul.Application.Finance.ProfitSharing.Mappings;

public static class ProfitSharingSettlementMappings
{
    public static ProfitSharingSettlementResponse ToResponse(
        this ProfitSharingSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        var allocations =
            settlement.Allocations
                .OrderBy(allocation =>
                    allocation.Sequence)
                .Select(allocation =>
                    allocation.ToResponse())
                .ToArray();

        return new ProfitSharingSettlementResponse(
            settlement.Id,
            settlement.OrganizationId,
            settlement.CropCycleId,
            settlement.Code,
            settlement.SettlementDate,
            settlement.ManagingPartnerCode,
            settlement.ManagingPartnerName,
            settlement.RecognizedRevenue,
            settlement.CollectedRevenue,
            settlement.OutstandingReceivable,
            settlement.ActivityResourceCost,
            settlement.ManualExpenseCost,
            settlement.TotalCultivationCost,
            settlement.NetProfit,
            settlement.Outcome,
            settlement.ManagementProfitPool,
            settlement.CapitalProfitPool,
            settlement.TotalInvestorCapital,
            settlement.TotalPartnerCapital,
            settlement.TotalCapital,
            settlement.TotalCapitalRecovery,
            settlement.TotalCapitalLoss,
            settlement.TotalInvestorProfitShare,
            settlement.TotalPartnerProfitShare,
            settlement.TotalPayout,
            settlement.CalculationVersion,
            settlement.Notes,
            settlement.Status,
            settlement.IsActive,
            settlement.FinalizedAt,
            settlement.VoidedAt,
            settlement.VoidReason,
            settlement.CreatedAt,
            settlement.UpdatedAt,
            Array.AsReadOnly(allocations));
    }

    public static ProfitSharingAllocationResponse ToResponse(
        this ProfitSharingAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        return new ProfitSharingAllocationResponse(
            allocation.Id,
            allocation.OrganizationId,
            allocation.ProfitSharingSettlementId,
            allocation.ContributorCodeSnapshot,
            allocation.ContributorNameSnapshot,
            allocation.ContributorRole,
            allocation.ConfirmedCapital,
            allocation.CapitalRatio,
            allocation.CapitalRecovery,
            allocation.CapitalLoss,
            allocation.ManagementProfitShare,
            allocation.CapitalProfitShare,
            allocation.TotalProfitShare,
            allocation.TotalPayout,
            allocation.Sequence,
            allocation.CreatedAt);
    }
}
