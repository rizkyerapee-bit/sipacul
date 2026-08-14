namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingWaterfallSchemeInput(
    IReadOnlyCollection<ProfitSharingWaterfallParticipantInput>
        Participants,
    IReadOnlyCollection<ProfitSharingPriorityRuleInput>
        PriorityRules,
    ProfitSharingResidualPolicyInput ResidualPolicy);
