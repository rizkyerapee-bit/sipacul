namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed class ProfitSharingSchemeResidualShare
{
    private ProfitSharingSchemeResidualShare()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSchemeId { get; private set; }

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

    internal static ProfitSharingSchemeResidualShare Create(
        Guid organizationId,
        Guid schemeId,
        ProfitSharingSchemeResidualShareDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ProfitSharingScheme.ValidatePositiveRate(
            definition.Rate,
            nameof(definition));

        ProfitSharingScheme.ValidateSequence(
            definition.Sequence,
            nameof(definition),
            "Residual share");

        return new ProfitSharingSchemeResidualShare
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSchemeId = schemeId,
            RecipientCode =
                ProfitSharingScheme.NormalizeCode(
                    definition.RecipientCode,
                    nameof(definition),
                    "Residual share recipient code"),
            RateNumerator = definition.Rate.Numerator,
            RateDenominator = definition.Rate.Denominator,
            Sequence = definition.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
