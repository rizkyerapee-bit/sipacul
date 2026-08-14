using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;

public sealed record ProfitSharingSchemePriorityRuleRequest(
    string RuleCode,
    ProfitSharingPriorityRuleType RuleType,
    string RecipientCode,
    decimal RateNumerator,
    decimal RateDenominator,
    int Sequence);
