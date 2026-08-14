using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

namespace SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;

public sealed record ProfitSharingPriorityAllocationPreviewResponse(
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

public sealed record ProfitSharingParticipantAllocationPreviewResponse(
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

public sealed record ProfitSharingPreviewTotalsResponse(
    decimal TotalCapital,
    decimal TotalCapitalRecovery,
    decimal TotalCapitalLoss,
    decimal TotalManagementProfitShare,
    decimal TotalReturnOnCapitalProfitShare,
    decimal TotalPriorityProfitShare,
    decimal TotalResidualProfitShare,
    decimal TotalProfitShare,
    decimal TotalPayout,
    ProfitSharingResidualMethod ResidualMethod);

public sealed record ProfitSharingPreviewResponse(
    Guid OrganizationId,
    Guid CropCycleId,
    bool IsPersisted,
    string CalculationVersion,
    DateTime GeneratedAt,
    ProfitSharingSchemeAssignmentResponse SchemeSnapshot,
    CropCycleProfitabilityResponse Profitability,
    ProfitSharingPreviewTotalsResponse Totals,
    IReadOnlyList<ProfitSharingPriorityAllocationPreviewResponse>
        PriorityAllocations,
    IReadOnlyList<ProfitSharingParticipantAllocationPreviewResponse>
        Allocations);
