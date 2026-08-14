namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

public sealed class ProfitSharingWaterfallPriorityAllocation
{
    private ProfitSharingWaterfallPriorityAllocation()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingWaterfallSettlementId
    {
        get;
        private set;
    }

    public string RuleCode { get; private set; } = string.Empty;

    public ProfitSharingPriorityRuleType RuleType
    {
        get;
        private set;
    }

    public string RecipientCodeSnapshot { get; private set; } =
        string.Empty;

    public string RecipientNameSnapshot { get; private set; } =
        string.Empty;

    public decimal RateNumerator { get; private set; }

    public decimal RateDenominator { get; private set; }

    public decimal BaseAmount { get; private set; }

    public decimal RequestedAmount { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    public decimal UnallocatedAmount { get; private set; }

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingWaterfallPriorityAllocation Create(
        Guid organizationId,
        Guid settlementId,
        ProfitSharingPriorityAllocationCalculation source,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfitSharingWaterfallPriorityAllocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingWaterfallSettlementId = settlementId,
            RuleCode = source.RuleCode,
            RuleType = source.RuleType,
            RecipientCodeSnapshot = source.RecipientCodeSnapshot,
            RecipientNameSnapshot = source.RecipientNameSnapshot,
            RateNumerator = source.Rate.Numerator,
            RateDenominator = source.Rate.Denominator,
            BaseAmount = source.BaseAmount,
            RequestedAmount = source.RequestedAmount,
            AllocatedAmount = source.AllocatedAmount,
            UnallocatedAmount = source.UnallocatedAmount,
            Sequence = source.Sequence,
            CreatedAt = createdAt
        };
    }
}
