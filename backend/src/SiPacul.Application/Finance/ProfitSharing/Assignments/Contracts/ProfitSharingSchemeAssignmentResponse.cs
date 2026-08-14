using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

namespace SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;

public sealed record
    ProfitSharingSchemeAssignmentParticipantResponse(
        Guid Id,
        string ParticipantCode,
        string ParticipantName,
        ProfitSharingParticipantRole ParticipantRole,
        bool ParticipatesInResidualProfit,
        int Sequence);

public sealed record
    ProfitSharingSchemeAssignmentPriorityRuleResponse(
        Guid Id,
        string RuleCode,
        ProfitSharingPriorityRuleType RuleType,
        string RecipientCode,
        decimal RateNumerator,
        decimal RateDenominator,
        int Sequence);

public sealed record
    ProfitSharingSchemeAssignmentResidualShareResponse(
        Guid Id,
        string RecipientCode,
        decimal RateNumerator,
        decimal RateDenominator,
        int Sequence);

public sealed record ProfitSharingSchemeAssignmentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    Guid SourceSchemeId,
    Guid SchemeFamilyId,
    string SchemeCode,
    string SchemeName,
    string? SchemeDescription,
    int SchemeVersion,
    ProfitSharingResidualMethod ResidualMethod,
    string? ResidualRecipientCode,
    DateTime AssignedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<
        ProfitSharingSchemeAssignmentParticipantResponse>
        Participants,
    IReadOnlyList<
        ProfitSharingSchemeAssignmentPriorityRuleResponse>
        PriorityRules,
    IReadOnlyList<
        ProfitSharingSchemeAssignmentResidualShareResponse>
        ResidualShares);
