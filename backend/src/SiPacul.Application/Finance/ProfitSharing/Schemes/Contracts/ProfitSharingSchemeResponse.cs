using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;

public sealed record ProfitSharingSchemeParticipantResponse(
    Guid Id,
    string ParticipantCode,
    string ParticipantName,
    ProfitSharingParticipantRole ParticipantRole,
    bool ParticipatesInResidualProfit,
    int Sequence);

public sealed record ProfitSharingSchemePriorityRuleResponse(
    Guid Id,
    string RuleCode,
    ProfitSharingPriorityRuleType RuleType,
    string RecipientCode,
    decimal RateNumerator,
    decimal RateDenominator,
    int Sequence);

public sealed record ProfitSharingSchemeResidualShareResponse(
    Guid Id,
    string RecipientCode,
    decimal RateNumerator,
    decimal RateDenominator,
    int Sequence);

public sealed record ProfitSharingSchemeResponse(
    Guid Id,
    Guid OrganizationId,
    Guid SchemeFamilyId,
    string Code,
    string Name,
    string? Description,
    int Version,
    ProfitSharingSchemeStatus Status,
    ProfitSharingResidualMethod ResidualMethod,
    string? ResidualRecipientCode,
    DateTime? ActivatedAt,
    DateTime? SupersededAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProfitSharingSchemeParticipantResponse>
        Participants,
    IReadOnlyList<ProfitSharingSchemePriorityRuleResponse>
        PriorityRules,
    IReadOnlyList<ProfitSharingSchemeResidualShareResponse>
        ResidualShares);
