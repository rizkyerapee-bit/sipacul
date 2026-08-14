using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

public sealed class ProfitSharingSchemeAssignmentPriorityRule
{
    private ProfitSharingSchemeAssignmentPriorityRule()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSchemeAssignmentId
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

    public string RecipientCode { get; private set; } =
        string.Empty;

    public decimal RateNumerator { get; private set; }

    public decimal RateDenominator { get; private set; }

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingSchemeAssignmentPriorityRule Create(
        Guid organizationId,
        Guid assignmentId,
        ProfitSharingSchemePriorityRule source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfitSharingSchemeAssignmentPriorityRule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSchemeAssignmentId = assignmentId,
            RuleCode = source.RuleCode,
            RuleType = source.RuleType,
            RecipientCode = source.RecipientCode,
            RateNumerator = source.RateNumerator,
            RateDenominator = source.RateDenominator,
            Sequence = source.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
