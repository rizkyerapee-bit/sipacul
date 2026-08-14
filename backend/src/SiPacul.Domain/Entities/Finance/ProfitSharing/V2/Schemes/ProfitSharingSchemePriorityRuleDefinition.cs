namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed record ProfitSharingSchemePriorityRuleDefinition(
    string RuleCode,
    ProfitSharingPriorityRuleType RuleType,
    string RecipientCode,
    ProfitSharingRate Rate,
    int Sequence);
