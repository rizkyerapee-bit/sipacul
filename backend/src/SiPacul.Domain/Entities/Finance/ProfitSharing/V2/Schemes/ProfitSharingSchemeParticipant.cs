namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed class ProfitSharingSchemeParticipant
{
    private ProfitSharingSchemeParticipant()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSchemeId { get; private set; }

    public string ParticipantCode { get; private set; } =
        string.Empty;

    public string ParticipantName { get; private set; } =
        string.Empty;

    public ProfitSharingParticipantRole ParticipantRole
    {
        get;
        private set;
    }

    public bool ParticipatesInResidualProfit
    {
        get;
        private set;
    }

    public int Sequence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static ProfitSharingSchemeParticipant Create(
        Guid organizationId,
        Guid schemeId,
        ProfitSharingSchemeParticipantDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!Enum.IsDefined(definition.ParticipantRole))
        {
            throw new ArgumentOutOfRangeException(
                nameof(definition),
                "Participant role is unsupported.");
        }

        ProfitSharingScheme.ValidateSequence(
            definition.Sequence,
            nameof(definition),
            "Participant");

        return new ProfitSharingSchemeParticipant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSchemeId = schemeId,
            ParticipantCode =
                ProfitSharingScheme.NormalizeCode(
                    definition.ParticipantCode,
                    nameof(definition),
                    "Participant code"),
            ParticipantName =
                ProfitSharingScheme.NormalizeRequiredText(
                    definition.ParticipantName,
                    ProfitSharingScheme.MaxParticipantNameLength,
                    nameof(definition),
                    "Participant name"),
            ParticipantRole = definition.ParticipantRole,
            ParticipatesInResidualProfit =
                definition.ParticipatesInResidualProfit,
            Sequence = definition.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
