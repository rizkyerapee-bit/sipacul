namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingPriorityAllocationCalculation(
    string RuleCode,
    ProfitSharingPriorityRuleType RuleType,
    string RecipientCodeSnapshot,
    string RecipientNameSnapshot,
    ProfitSharingRate Rate,
    decimal BaseAmount,
    decimal RequestedAmount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    int Sequence);
