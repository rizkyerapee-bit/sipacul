using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

public sealed class ProfitSharingSchemeAssignmentResidualShare
{
    private ProfitSharingSchemeAssignmentResidualShare()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSchemeAssignmentId
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

    internal static ProfitSharingSchemeAssignmentResidualShare Create(
        Guid organizationId,
        Guid assignmentId,
        ProfitSharingSchemeResidualShare source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfitSharingSchemeAssignmentResidualShare
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSchemeAssignmentId = assignmentId,
            RecipientCode = source.RecipientCode,
            RateNumerator = source.RateNumerator,
            RateDenominator = source.RateDenominator,
            Sequence = source.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
