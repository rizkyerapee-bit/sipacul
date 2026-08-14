using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed partial class ProfitSharingScheme :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxNameLength = 150;

    public const int MaxDescriptionLength = 1000;

    public const int MaxParticipantNameLength = 150;

    private const decimal RateTolerance =
        0.00000001m;

    private readonly List<ProfitSharingSchemeParticipant>
        _participants = [];

    private readonly List<ProfitSharingSchemePriorityRule>
        _priorityRules = [];

    private readonly List<ProfitSharingSchemeResidualShare>
        _residualShares = [];

    private ProfitSharingScheme()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid SchemeFamilyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int Version { get; private set; }

    public ProfitSharingSchemeStatus Status
    {
        get;
        private set;
    } = ProfitSharingSchemeStatus.Draft;

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

    public DateTime? ActivatedAt { get; private set; }

    public DateTime? SupersededAt { get; private set; }

    public IReadOnlyCollection<ProfitSharingSchemeParticipant>
        Participants =>
            _participants.AsReadOnly();

    public IReadOnlyCollection<ProfitSharingSchemePriorityRule>
        PriorityRules =>
            _priorityRules.AsReadOnly();

    public IReadOnlyCollection<ProfitSharingSchemeResidualShare>
        ResidualShares =>
            _residualShares.AsReadOnly();

    public static ProfitSharingScheme CreateDraft(
        Guid organizationId,
        string code,
        string name,
        string? description,
        IReadOnlyCollection<
            ProfitSharingSchemeParticipantDefinition> participants,
        IReadOnlyCollection<
            ProfitSharingSchemePriorityRuleDefinition> priorityRules,
        ProfitSharingResidualMethod residualMethod,
        string? residualRecipientCode,
        IReadOnlyCollection<
            ProfitSharingSchemeResidualShareDefinition> residualShares)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        var scheme =
            new ProfitSharingScheme
            {
                OrganizationId = organizationId,
                Code =
                    NormalizeCode(
                        code,
                        nameof(code),
                        "Scheme code"),
                Name =
                    NormalizeRequiredText(
                        name,
                        MaxNameLength,
                        nameof(name),
                        "Scheme name"),
                Description =
                    NormalizeOptionalText(
                        description,
                        MaxDescriptionLength,
                        nameof(description)),
                Version = 1,
                Status = ProfitSharingSchemeStatus.Draft
            };

        scheme.SchemeFamilyId = scheme.Id;

        scheme.ReplaceConfiguration(
            participants,
            priorityRules,
            residualMethod,
            residualRecipientCode,
            residualShares,
            markUpdated: false);

        return scheme;
    }

    public static ProfitSharingScheme CreateNextVersion(
        ProfitSharingScheme source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Status != ProfitSharingSchemeStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active scheme can create the next version.");
        }

        source.EnsureStoredInvariants();

        var next =
            new ProfitSharingScheme
            {
                OrganizationId = source.OrganizationId,
                SchemeFamilyId = source.SchemeFamilyId,
                Code = source.Code,
                Name = source.Name,
                Description = source.Description,
                Version = checked(source.Version + 1),
                Status = ProfitSharingSchemeStatus.Draft
            };

        next.ReplaceConfiguration(
            source.Participants
                .OrderBy(participant => participant.Sequence)
                .Select(participant =>
                    new ProfitSharingSchemeParticipantDefinition(
                        participant.ParticipantCode,
                        participant.ParticipantName,
                        participant.ParticipantRole,
                        participant.ParticipatesInResidualProfit,
                        participant.Sequence))
                .ToArray(),
            source.PriorityRules
                .OrderBy(rule => rule.Sequence)
                .Select(rule =>
                    new ProfitSharingSchemePriorityRuleDefinition(
                        rule.RuleCode,
                        rule.RuleType,
                        rule.RecipientCode,
                        rule.Rate,
                        rule.Sequence))
                .ToArray(),
            source.ResidualMethod,
            source.ResidualRecipientCode,
            source.ResidualShares
                .OrderBy(share => share.Sequence)
                .Select(share =>
                    new ProfitSharingSchemeResidualShareDefinition(
                        share.RecipientCode,
                        share.Rate,
                        share.Sequence))
                .ToArray(),
            markUpdated: false);

        return next;
    }

    public void UpdateDraft(
        string name,
        string? description,
        IReadOnlyCollection<
            ProfitSharingSchemeParticipantDefinition> participants,
        IReadOnlyCollection<
            ProfitSharingSchemePriorityRuleDefinition> priorityRules,
        ProfitSharingResidualMethod residualMethod,
        string? residualRecipientCode,
        IReadOnlyCollection<
            ProfitSharingSchemeResidualShareDefinition> residualShares)
    {
        EnsureDraft();

        var normalizedName =
            NormalizeRequiredText(
                name,
                MaxNameLength,
                nameof(name),
                "Scheme name");

        var normalizedDescription =
            NormalizeOptionalText(
                description,
                MaxDescriptionLength,
                nameof(description));

        ReplaceConfiguration(
            participants,
            priorityRules,
            residualMethod,
            residualRecipientCode,
            residualShares,
            markUpdated: true);

        Name = normalizedName;
        Description = normalizedDescription;
    }

    public void Activate()
    {
        EnsureDraft();
        EnsureStoredInvariants();

        var now = DateTime.UtcNow;

        Status = ProfitSharingSchemeStatus.Active;
        ActivatedAt = now;
        SupersededAt = null;
        UpdatedAt = now;
    }

    public void Supersede()
    {
        if (Status != ProfitSharingSchemeStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active scheme can be superseded.");
        }

        var now = DateTime.UtcNow;

        Status = ProfitSharingSchemeStatus.Superseded;
        SupersededAt = now;
        UpdatedAt = now;
    }

    private void ReplaceConfiguration(
        IReadOnlyCollection<
            ProfitSharingSchemeParticipantDefinition> participants,
        IReadOnlyCollection<
            ProfitSharingSchemePriorityRuleDefinition> priorityRules,
        ProfitSharingResidualMethod residualMethod,
        string? residualRecipientCode,
        IReadOnlyCollection<
            ProfitSharingSchemeResidualShareDefinition> residualShares,
        bool markUpdated)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(priorityRules);
        ArgumentNullException.ThrowIfNull(residualShares);

        if (!Enum.IsDefined(residualMethod))
        {
            throw new ArgumentOutOfRangeException(
                nameof(residualMethod),
                "Residual method is unsupported.");
        }

        var normalizedParticipants =
            participants
                .Select(participant =>
                    ProfitSharingSchemeParticipant.Create(
                        OrganizationId,
                        Id,
                        participant))
                .OrderBy(participant => participant.Sequence)
                .ToArray();

        var normalizedPriorityRules =
            priorityRules
                .Select(rule =>
                    ProfitSharingSchemePriorityRule.Create(
                        OrganizationId,
                        Id,
                        rule))
                .OrderBy(rule => rule.Sequence)
                .ToArray();

        var normalizedResidualShares =
            residualShares
                .Select(share =>
                    ProfitSharingSchemeResidualShare.Create(
                        OrganizationId,
                        Id,
                        share))
                .OrderBy(share => share.Sequence)
                .ToArray();

        var normalizedRecipientCode =
            string.IsNullOrWhiteSpace(residualRecipientCode)
                ? null
                : NormalizeCode(
                    residualRecipientCode,
                    nameof(residualRecipientCode),
                    "Residual recipient code");

        ValidateConfiguration(
            normalizedParticipants,
            normalizedPriorityRules,
            residualMethod,
            normalizedRecipientCode,
            normalizedResidualShares);

        _participants.Clear();
        _participants.AddRange(normalizedParticipants);

        _priorityRules.Clear();
        _priorityRules.AddRange(normalizedPriorityRules);

        _residualShares.Clear();
        _residualShares.AddRange(normalizedResidualShares);

        ResidualMethod = residualMethod;
        ResidualRecipientCode = normalizedRecipientCode;

        if (markUpdated)
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private void EnsureStoredInvariants()
    {
        ValidateConfiguration(
            _participants
                .OrderBy(participant => participant.Sequence)
                .ToArray(),
            _priorityRules
                .OrderBy(rule => rule.Sequence)
                .ToArray(),
            ResidualMethod,
            ResidualRecipientCode,
            _residualShares
                .OrderBy(share => share.Sequence)
                .ToArray());
    }

    private static void ValidateConfiguration(
        IReadOnlyList<ProfitSharingSchemeParticipant> participants,
        IReadOnlyList<ProfitSharingSchemePriorityRule> priorityRules,
        ProfitSharingResidualMethod residualMethod,
        string? residualRecipientCode,
        IReadOnlyList<ProfitSharingSchemeResidualShare> residualShares)
    {
        if (participants.Count == 0)
        {
            throw new ArgumentException(
                "At least one scheme participant is required.",
                nameof(participants));
        }

        EnsureContiguousSequence(
            participants.Select(participant =>
                participant.Sequence),
            nameof(participants),
            "Participant");

        if (participants
            .GroupBy(
                participant => participant.ParticipantCode,
                StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Participant codes must be unique.",
                nameof(participants));
        }

        EnsureContiguousSequence(
            priorityRules.Select(rule => rule.Sequence),
            nameof(priorityRules),
            "Priority rule");

        if (priorityRules
            .GroupBy(
                rule => rule.RuleCode,
                StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Priority rule codes must be unique.",
                nameof(priorityRules));
        }

        var participantCodes =
            participants
                .Select(participant =>
                    participant.ParticipantCode)
                .ToHashSet(StringComparer.Ordinal);

        if (priorityRules.Any(rule =>
                !participantCodes.Contains(rule.RecipientCode)))
        {
            throw new ArgumentException(
                "Every priority rule recipient must exist " +
                "in the participant list.",
                nameof(priorityRules));
        }

        switch (residualMethod)
        {
            case ProfitSharingResidualMethod
                .RemainderToParticipant:
                if (residualRecipientCode is null ||
                    !participantCodes.Contains(
                        residualRecipientCode))
                {
                    throw new ArgumentException(
                        "Residual recipient must exist in the " +
                        "participant list.",
                        nameof(residualRecipientCode));
                }

                if (residualShares.Count != 0)
                {
                    throw new ArgumentException(
                        "Remainder policy cannot contain fixed " +
                        "shares.",
                        nameof(residualShares));
                }

                break;

            case ProfitSharingResidualMethod.ProRataCapital:
                if (residualRecipientCode is not null ||
                    residualShares.Count != 0)
                {
                    throw new ArgumentException(
                        "Pro-rata policy cannot contain a residual " +
                        "recipient or fixed shares.",
                        nameof(residualMethod));
                }

                if (!participants.Any(participant =>
                        participant.ParticipatesInResidualProfit))
                {
                    throw new ArgumentException(
                        "Pro-rata policy requires at least one " +
                        "residual participant.",
                        nameof(participants));
                }

                break;

            case ProfitSharingResidualMethod.FixedPercentage:
                if (residualRecipientCode is not null)
                {
                    throw new ArgumentException(
                        "Fixed-percentage policy cannot contain a " +
                        "single recipient.",
                        nameof(residualRecipientCode));
                }

                ValidateFixedResidualShares(
                    residualShares,
                    participantCodes);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(residualMethod),
                    "Residual method is unsupported.");
        }
    }

    private static void ValidateFixedResidualShares(
        IReadOnlyList<ProfitSharingSchemeResidualShare> shares,
        IReadOnlySet<string> participantCodes)
    {
        if (shares.Count == 0)
        {
            throw new ArgumentException(
                "Fixed-percentage policy requires shares.",
                nameof(shares));
        }

        EnsureContiguousSequence(
            shares.Select(share => share.Sequence),
            nameof(shares),
            "Residual share");

        if (shares
            .GroupBy(
                share => share.RecipientCode,
                StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Residual share recipients must be unique.",
                nameof(shares));
        }

        if (shares.Any(share =>
                !participantCodes.Contains(share.RecipientCode)))
        {
            throw new ArgumentException(
                "Every residual share recipient must exist " +
                "in the participant list.",
                nameof(shares));
        }

        var totalRate = shares.Sum(share => share.Rate.Value);

        if (Math.Abs(totalRate - 1m) > RateTolerance)
        {
            throw new ArgumentException(
                "Fixed residual percentages must total 100%.",
                nameof(shares));
        }
    }

    private void EnsureDraft()
    {
        if (Status != ProfitSharingSchemeStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft scheme can be changed.");
        }
    }

    private static void EnsureContiguousSequence(
        IEnumerable<int> sequences,
        string parameterName,
        string displayName)
    {
        var values = sequences.ToArray();

        if (!values.SequenceEqual(
                Enumerable.Range(1, values.Length)))
        {
            throw new ArgumentException(
                $"{displayName} sequence must be contiguous.",
                parameterName);
        }
    }

    internal static void ValidatePositiveRate(
        ProfitSharingRate rate,
        string parameterName)
    {
        if (rate.Denominator <= 0 ||
            rate.Numerator <= 0 ||
            rate.Numerator > rate.Denominator)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Rate must be greater than zero and no greater " +
                "than one.");
        }
    }

    internal static void ValidateSequence(
        int sequence,
        string parameterName,
        string displayName)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"{displayName} sequence must be positive.");
        }
    }

    private static void ValidateIdentifier(
        Guid value,
        string parameterName,
        string displayName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                $"{displayName} identifier cannot be empty.",
                parameterName);
        }
    }

    internal static string NormalizeCode(
        string value,
        string parameterName,
        string displayName)
    {
        var normalized =
            NormalizeRequiredText(
                    value,
                    MaxCodeLength,
                    parameterName,
                    displayName)
                .ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"{displayName} format is invalid.",
                parameterName);
        }

        return normalized;
    }

    internal static string NormalizeRequiredText(
        string value,
        int maximumLength,
        string parameterName,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9._-]{0,39}$")]
    private static partial Regex CodePattern();
}
