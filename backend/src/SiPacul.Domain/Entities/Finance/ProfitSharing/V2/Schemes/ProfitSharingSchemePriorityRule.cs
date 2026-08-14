namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed class ProfitSharingSchemePriorityRule
{
    private ProfitSharingSchemePriorityRule()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSchemeId { get; private set; }

    public string RuleCode { get; private set; } =
        string.Empty;

    public ProfitSharingPriorityRuleType RuleType
    {
        get;
        private set;
    }

    public string RecipientCode { get; private set; } =
        string.Empty;

    public decimal RateNumerator { get; private set; }

    public decimal RateDenominator { get; private set; }

    public ProfitSharingRate Rate =>
        ProfitSharingRate.FromFraction(
            RateNumerator,
            RateDenominator);

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingSchemePriorityRule Create(
        Guid organizationId,
        Guid schemeId,
        ProfitSharingSchemePriorityRuleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!Enum.IsDefined(definition.RuleType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(definition),
                "Priority rule type is unsupported.");
        }

        ProfitSharingScheme.ValidatePositiveRate(
            definition.Rate,
            nameof(definition));

        ProfitSharingScheme.ValidateSequence(
            definition.Sequence,
            nameof(definition),
            "Priority rule");

        return new ProfitSharingSchemePriorityRule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSchemeId = schemeId,
            RuleCode =
                ProfitSharingScheme.NormalizeCode(
                    definition.RuleCode,
                    nameof(definition),
                    "Priority rule code"),
            RuleType = definition.RuleType,
            RecipientCode =
                ProfitSharingScheme.NormalizeCode(
                    definition.RecipientCode,
                    nameof(definition),
                    "Priority rule recipient code"),
            RateNumerator = definition.Rate.Numerator,
            RateDenominator = definition.Rate.Denominator,
            Sequence = definition.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
