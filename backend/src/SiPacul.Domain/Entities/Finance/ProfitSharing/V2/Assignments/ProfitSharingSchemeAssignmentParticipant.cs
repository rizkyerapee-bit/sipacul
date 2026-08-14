using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

public sealed class ProfitSharingSchemeAssignmentParticipant
{
    private ProfitSharingSchemeAssignmentParticipant()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProfitSharingSchemeAssignmentId
    {
        get;
        private set;
    }

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

    internal static ProfitSharingSchemeAssignmentParticipant Create(
        Guid organizationId,
        Guid assignmentId,
        ProfitSharingSchemeParticipant source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new ProfitSharingSchemeAssignmentParticipant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProfitSharingSchemeAssignmentId = assignmentId,
            ParticipantCode = source.ParticipantCode,
            ParticipantName = source.ParticipantName,
            ParticipantRole = source.ParticipantRole,
            ParticipatesInResidualProfit =
                source.ParticipatesInResidualProfit,
            Sequence = source.Sequence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
