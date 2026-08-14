namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingPriorityRuleInput(
    string RuleCode,
    ProfitSharingPriorityRuleType RuleType,
    string RecipientCode,
    ProfitSharingRate Rate,
    int Sequence);
