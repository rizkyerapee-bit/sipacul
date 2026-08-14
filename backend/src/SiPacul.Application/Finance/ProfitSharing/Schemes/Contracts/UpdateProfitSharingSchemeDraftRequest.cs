using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;

public sealed record UpdateProfitSharingSchemeDraftRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<ProfitSharingSchemeParticipantRequest>
        Participants,
    IReadOnlyCollection<ProfitSharingSchemePriorityRuleRequest>
        PriorityRules,
    ProfitSharingResidualMethod ResidualMethod,
    string? ResidualRecipientCode,
    IReadOnlyCollection<ProfitSharingSchemeResidualShareRequest>
        ResidualShares);
