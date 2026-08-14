using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingWaterfallCalculationResult(
    Guid OrganizationId,
    Guid CropCycleId,
    decimal RecognizedRevenue,
    decimal TotalCultivationCost,
    decimal NetProfit,
    ProfitabilityOutcome Outcome,
    decimal TotalCapital,
    decimal TotalCapitalRecovery,
    decimal TotalCapitalLoss,
    decimal TotalManagementProfitShare,
    decimal TotalReturnOnCapitalProfitShare,
    decimal TotalPriorityProfitShare,
    decimal TotalResidualProfitShare,
    decimal TotalProfitShare,
    decimal TotalPayout,
    ProfitSharingResidualMethod ResidualMethod,
    string CalculationVersion,
    IReadOnlyList<ProfitSharingPriorityAllocationCalculation>
        PriorityAllocations,
    IReadOnlyList<ProfitSharingWaterfallAllocationCalculation>
        Allocations);
