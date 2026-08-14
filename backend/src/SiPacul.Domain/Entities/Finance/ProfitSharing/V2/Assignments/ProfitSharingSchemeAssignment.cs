using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

public sealed class ProfitSharingSchemeAssignment :
    AggregateRoot,
    IOrganizationOwned
{
    private readonly List<
        ProfitSharingSchemeAssignmentParticipant>
        _participants = [];

    private readonly List<
        ProfitSharingSchemeAssignmentPriorityRule>
        _priorityRules = [];

    private readonly List<
        ProfitSharingSchemeAssignmentResidualShare>
        _residualShares = [];

    private ProfitSharingSchemeAssignment()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public Guid SourceSchemeId { get; private set; }

    public Guid SchemeFamilyId { get; private set; }

    public string SchemeCode { get; private set; } = string.Empty;

    public string SchemeName { get; private set; } = string.Empty;

    public string? SchemeDescription { get; private set; }

    public int SchemeVersion { get; private set; }

    public ProfitSharingResidualMethod ResidualMethod
    {
        get;
        private set;
    }

    public string? ResidualRecipientCode
    {
        get;
        private set;
    }

    public DateTime AssignedAt { get; private set; }

    public IReadOnlyCollection<
        ProfitSharingSchemeAssignmentParticipant>
        Participants =>
            _participants.AsReadOnly();

    public IReadOnlyCollection<
        ProfitSharingSchemeAssignmentPriorityRule>
        PriorityRules =>
            _priorityRules.AsReadOnly();

    public IReadOnlyCollection<
        ProfitSharingSchemeAssignmentResidualShare>
        ResidualShares =>
            _residualShares.AsReadOnly();

    public static ProfitSharingSchemeAssignment Create(
        Guid organizationId,
        Guid cropCycleId,
        ProfitSharingScheme scheme)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            cropCycleId,
            nameof(cropCycleId),
            "Crop cycle");

        var assignment =
            new ProfitSharingSchemeAssignment
            {
                OrganizationId = organizationId,
                CropCycleId = cropCycleId
            };

        assignment.ApplySnapshot(
            scheme,
            markUpdated: false);

        return assignment;
    }

    public void ReplaceSnapshot(ProfitSharingScheme scheme)
    {
        ApplySnapshot(
            scheme,
            markUpdated: true);
    }

    private void ApplySnapshot(
        ProfitSharingScheme scheme,
        bool markUpdated)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        if (scheme.OrganizationId != OrganizationId)
        {
            throw new ArgumentException(
                "Scheme organization must match the assignment " +
                "organization.",
                nameof(scheme));
        }

        if (scheme.Status != ProfitSharingSchemeStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active profit sharing scheme can be " +
                "assigned to a crop cycle.");
        }

        if (scheme.Version <= 0)
        {
            throw new ArgumentException(
                "Scheme version must be greater than zero.",
                nameof(scheme));
        }

        var participants = scheme.Participants
            .OrderBy(participant => participant.Sequence)
            .Select(participant =>
                ProfitSharingSchemeAssignmentParticipant.Create(
                    OrganizationId,
                    Id,
                    participant))
            .ToArray();

        var priorityRules = scheme.PriorityRules
            .OrderBy(rule => rule.Sequence)
            .Select(rule =>
                ProfitSharingSchemeAssignmentPriorityRule.Create(
                    OrganizationId,
                    Id,
                    rule))
            .ToArray();

        var residualShares = scheme.ResidualShares
            .OrderBy(share => share.Sequence)
            .Select(share =>
                ProfitSharingSchemeAssignmentResidualShare.Create(
                    OrganizationId,
                    Id,
                    share))
            .ToArray();

        if (participants.Length == 0)
        {
            throw new ArgumentException(
                "Scheme snapshot must contain at least one " +
                "participant.",
                nameof(scheme));
        }

        var assignedAt = DateTime.UtcNow;

        SourceSchemeId = scheme.Id;
        SchemeFamilyId = scheme.SchemeFamilyId;
        SchemeCode = scheme.Code;
        SchemeName = scheme.Name;
        SchemeDescription = scheme.Description;
        SchemeVersion = scheme.Version;
        ResidualMethod = scheme.ResidualMethod;
        ResidualRecipientCode = scheme.ResidualRecipientCode;
        AssignedAt = assignedAt;

        _participants.Clear();
        _participants.AddRange(participants);

        _priorityRules.Clear();
        _priorityRules.AddRange(priorityRules);

        _residualShares.Clear();
        _residualShares.AddRange(residualShares);

        if (markUpdated)
        {
            UpdatedAt = assignedAt;
        }
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string displayName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                $"{displayName} identifier cannot be empty.",
                parameterName);
        }
    }
}
