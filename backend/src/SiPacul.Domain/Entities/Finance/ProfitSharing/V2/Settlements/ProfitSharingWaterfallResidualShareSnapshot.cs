using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

public sealed class ProfitSharingWaterfallResidualShareSnapshot
{
    private ProfitSharingWaterfallResidualShareSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingWaterfallSettlementId
    {
        get;
        private set;
    }

    public string RecipientCodeSnapshot { get; private set; } =
        string.Empty;

    public decimal RateNumerator { get; private set; }

    public decimal RateDenominator { get; private set; }

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingWaterfallResidualShareSnapshot Create(
        Guid organizationId,
        Guid settlementId,
        ProfitSharingSchemeAssignmentResidualShare source,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfitSharingWaterfallResidualShareSnapshot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingWaterfallSettlementId = settlementId,
            RecipientCodeSnapshot = source.RecipientCode,
            RateNumerator = source.RateNumerator,
            RateDenominator = source.RateDenominator,
            Sequence = source.Sequence,
            CreatedAt = createdAt
        };
    }
}
