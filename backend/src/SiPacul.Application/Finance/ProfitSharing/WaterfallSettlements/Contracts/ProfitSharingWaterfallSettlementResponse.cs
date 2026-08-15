using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;

public sealed record ProfitSharingWaterfallPriorityAllocationResponse(
    Guid Id,
    string RuleCode,
    ProfitSharingPriorityRuleType RuleType,
    string RecipientCodeSnapshot,
    string RecipientNameSnapshot,
    decimal RateNumerator,
    decimal RateDenominator,
    decimal BaseAmount,
    decimal RequestedAmount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    int Sequence);

public sealed record ProfitSharingWaterfallParticipantAllocationResponse(
    Guid Id,
    string ParticipantCodeSnapshot,
    string ParticipantNameSnapshot,
    ProfitSharingParticipantRole ParticipantRole,
    decimal ConfirmedCapital,
    decimal CapitalRatio,
    bool ParticipatesInResidualProfit,
    decimal CapitalRecovery,
    decimal CapitalLoss,
    decimal ManagementProfitShare,
    decimal ReturnOnCapitalProfitShare,
    decimal ResidualProfitShare,
    decimal TotalProfitShare,
    decimal TotalPayout,
    int Sequence);

public sealed record ProfitSharingWaterfallResidualShareResponse(
    Guid Id,
    string RecipientCodeSnapshot,
    decimal RateNumerator,
    decimal RateDenominator,
    int Sequence);

public sealed record ProfitSharingWaterfallSettlementResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    Guid AssignmentId,
    Guid SourceSchemeId,
    Guid SchemeFamilyId,
    string Code,
    DateOnly SettlementDate,
    string SchemeCodeSnapshot,
    string SchemeNameSnapshot,
    string? SchemeDescriptionSnapshot,
    int SchemeVersionSnapshot,
    DateTime SchemeAssignedAtSnapshot,
    ProfitSharingResidualMethod ResidualMethod,
    string? ResidualRecipientCodeSnapshot,
    string CropCycleCodeSnapshot,
    string CropCycleNameSnapshot,
    Guid CommodityIdSnapshot,
    string CommodityCodeSnapshot,
    string CommodityNameSnapshot,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal OutstandingReceivable,
    decimal ActivityResourceCost,
    decimal ManualExpenseCost,
    decimal TotalCultivationCost,
    decimal NetProfit,
    ProfitabilityOutcome Outcome,
    decimal ConfirmedInvestorCapital,
    decimal ConfirmedPartnerCapital,
    decimal TotalConfirmedCapital,
    decimal AvailableHarvestQuantity,
    decimal TotalCapital,
    decimal TotalCapitalRecovery,
    decimal TotalCapitalLoss,
    decimal TotalManagementProfitShare,
    decimal TotalReturnOnCapitalProfitShare,
    decimal TotalPriorityProfitShare,
    decimal TotalResidualProfitShare,
    decimal TotalProfitShare,
    decimal TotalPayout,
    string CalculationVersion,
    DateTime CalculatedAt,
    string? Notes,
    ProfitSharingWaterfallSettlementStatus Status,
    DateTime FinalizedAt,
    DateTime? VoidedAt,
    string? VoidReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProfitSharingWaterfallPriorityAllocationResponse>
        PriorityAllocations,
    IReadOnlyList<ProfitSharingWaterfallParticipantAllocationResponse>
        ParticipantAllocations,
    IReadOnlyList<ProfitSharingWaterfallResidualShareResponse>
        ResidualShares);
