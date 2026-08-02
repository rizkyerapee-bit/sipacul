using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing;

public sealed record ProfitSharingCalculationResult(
    Guid OrganizationId,
    Guid CropCycleId,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal OutstandingReceivable,
    decimal ActivityResourceCost,
    decimal ManualExpenseCost,
    decimal TotalCultivationCost,
    decimal NetProfit,
    ProfitabilityOutcome Outcome,
    decimal ManagementProfitPool,
    decimal CapitalProfitPool,
    decimal TotalInvestorCapital,
    decimal TotalPartnerCapital,
    decimal TotalCapital,
    decimal TotalCapitalRecovery,
    decimal TotalCapitalLoss,
    decimal TotalInvestorProfitShare,
    decimal TotalPartnerProfitShare,
    decimal TotalPayout,
    string CalculationVersion,
    IReadOnlyList<ProfitSharingAllocationCalculation>
        Allocations);
