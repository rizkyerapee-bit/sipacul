using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Cultivation;

public sealed partial class CultivationActivity :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxNameLength = 150;

    public const int MaxSopStepNameLength = 150;

    public const int MaxOutcomeLength = 1000;

    public const int MaxIssueNotesLength = 1000;

    public const int MaxDeviationReasonLength = 500;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private readonly List<CultivationActivityResource>
        _resources = [];

    private CultivationActivity()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public string Name { get; private set; } =
        string.Empty;

    public CultivationActivityType ActivityType
    {
        get;
        private set;
    }

    public Guid? CultivationSopId { get; private set; }

    public Guid? CultivationSopStepId
    {
        get;
        private set;
    }

    public int? SopStepSequenceSnapshot
    {
        get;
        private set;
    }

    public string? SopStepNameSnapshot
    {
        get;
        private set;
    }

    public int? SopPlannedDayOffsetSnapshot
    {
        get;
        private set;
    }

    public int? SopEstimatedDurationDaysSnapshot
    {
        get;
        private set;
    }

    public bool? SopIsRequiredSnapshot
    {
        get;
        private set;
    }

    public DateOnly PlannedDate { get; private set; }

    public DateOnly? ActualStartDate
    {
        get;
        private set;
    }

    public DateOnly? ActualCompletionDate
    {
        get;
        private set;
    }

    public CultivationActivityStatus Status
    {
        get;
        private set;
    } = CultivationActivityStatus.Planned;

    public SopComplianceStatus SopComplianceStatus
    {
        get;
        private set;
    } = SopComplianceStatus.NotApplicable;

    public string? Outcome { get; private set; }

    public string? IssueNotes { get; private set; }

    public string? DeviationReason { get; private set; }

    public string? CancellationReason { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<
        CultivationActivityResource> Resources =>
        _resources.AsReadOnly();

    public decimal TotalActualCost =>
        _resources.Sum(resource =>
            resource.TotalCost);

    public bool IsLinkedToSopStep =>
        CultivationSopId.HasValue;

    public static CultivationActivity Create(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        string name,
        CultivationActivityType activityType,
        DateOnly plannedDate,
        Guid? cultivationSopId,
        Guid? cultivationSopStepId,
        int? sopStepSequenceSnapshot,
        string? sopStepNameSnapshot,
        int? sopPlannedDayOffsetSnapshot,
        int? sopEstimatedDurationDaysSnapshot,
        bool? sopIsRequiredSnapshot,
        string? notes)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            cropCycleId,
            nameof(cropCycleId),
            "Crop cycle");

        ValidateActivityType(activityType);
        ValidateDate(
            plannedDate,
            nameof(plannedDate),
            "Planned date");

        var snapshot = ValidateSopSnapshot(
            cultivationSopId,
            cultivationSopStepId,
            sopStepSequenceSnapshot,
            sopStepNameSnapshot,
            sopPlannedDayOffsetSnapshot,
            sopEstimatedDurationDaysSnapshot,
            sopIsRequiredSnapshot);

        return new CultivationActivity
        {
            OrganizationId = organizationId,
            CropCycleId = cropCycleId,
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            ActivityType = activityType,
            CultivationSopId =
                snapshot.CultivationSopId,
            CultivationSopStepId =
                snapshot.CultivationSopStepId,
            SopStepSequenceSnapshot =
                snapshot.Sequence,
            SopStepNameSnapshot = snapshot.Name,
            SopPlannedDayOffsetSnapshot =
                snapshot.PlannedDayOffset,
            SopEstimatedDurationDaysSnapshot =
                snapshot.EstimatedDurationDays,
            SopIsRequiredSnapshot =
                snapshot.IsRequired,
            PlannedDate = plannedDate,
            Status =
                CultivationActivityStatus.Planned,
            SopComplianceStatus =
                snapshot.CultivationSopId.HasValue
                    ? SopComplianceStatus.NotEvaluated
                    : SopComplianceStatus.NotApplicable,
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes))
        };
    }

    public void UpdatePlan(
        string name,
        CultivationActivityType activityType,
        DateOnly plannedDate,
        string? notes)
    {
        EnsureStatus(
            CultivationActivityStatus.Planned,
            "Only a planned cultivation activity " +
            "can be updated.");

        ValidateActivityType(activityType);
        ValidateDate(
            plannedDate,
            nameof(plannedDate),
            "Planned date");

        var normalizedName = NormalizeName(name);

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (Name == normalizedName &&
            ActivityType == activityType &&
            PlannedDate == plannedDate &&
            Notes == normalizedNotes)
        {
            return;
        }

        Name = normalizedName;
        ActivityType = activityType;
        PlannedDate = plannedDate;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start(DateOnly actualStartDate)
    {
        EnsureStatus(
            CultivationActivityStatus.Planned,
            "Only a planned cultivation activity " +
            "can be started.");

        ValidateDate(
            actualStartDate,
            nameof(actualStartDate),
            "Actual start date");

        ActualStartDate = actualStartDate;
        Status = CultivationActivityStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(
        DateOnly actualCompletionDate,
        string? outcome,
        string? issueNotes,
        SopComplianceStatus sopComplianceStatus,
        string? deviationReason)
    {
        EnsureStatus(
            CultivationActivityStatus.InProgress,
            "Only an in-progress cultivation activity " +
            "can be completed.");

        ValidateDate(
            actualCompletionDate,
            nameof(actualCompletionDate),
            "Actual completion date");

        if (!ActualStartDate.HasValue)
        {
            throw new InvalidOperationException(
                "An in-progress cultivation activity " +
                "must have an actual start date.");
        }

        if (actualCompletionDate <
            ActualStartDate.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualCompletionDate),
                "Actual completion date cannot be before " +
                "the actual start date.");
        }

        var compliance =
            ValidateCompletionCompliance(
                sopComplianceStatus,
                deviationReason);

        ActualCompletionDate =
            actualCompletionDate;

        Outcome = NormalizeOptionalText(
            outcome,
            MaxOutcomeLength,
            nameof(outcome));

        IssueNotes = NormalizeOptionalText(
            issueNotes,
            MaxIssueNotesLength,
            nameof(issueNotes));

        SopComplianceStatus =
            compliance.Status;

        DeviationReason =
            compliance.DeviationReason;

        Status = CultivationActivityStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        EnsureMutableStatus(
            "Only a planned or in-progress cultivation " +
            "activity can be cancelled.");

        CancellationReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        Status = CultivationActivityStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateExecutionNotes(
        string? notes,
        string? issueNotes)
    {
        EnsureMutableStatus(
            "Execution notes cannot be updated on a " +
            "terminal cultivation activity.");

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        var normalizedIssueNotes =
            NormalizeOptionalText(
                issueNotes,
                MaxIssueNotesLength,
                nameof(issueNotes));

        if (Notes == normalizedNotes &&
            IssueNotes == normalizedIssueNotes)
        {
            return;
        }

        Notes = normalizedNotes;
        IssueNotes = normalizedIssueNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public CultivationActivityResource AddResource(
        CultivationResourceType resourceType,
        string description,
        decimal quantity,
        string unit,
        decimal unitCost,
        string? notes)
    {
        EnsureMutableStatus(
            "Resources cannot be added to a terminal " +
            "cultivation activity.");

        var resource =
            CultivationActivityResource.Create(
                OrganizationId,
                Id,
                resourceType,
                description,
                quantity,
                unit,
                unitCost,
                notes);

        _resources.Add(resource);
        UpdatedAt = DateTime.UtcNow;

        return resource;
    }

    public void UpdateResource(
        Guid resourceId,
        string description,
        decimal quantity,
        string unit,
        decimal unitCost,
        string? notes)
    {
        EnsureMutableStatus(
            "Resources cannot be updated on a terminal " +
            "cultivation activity.");

        var resource = FindResource(resourceId);

        if (resource.Update(
                description,
                quantity,
                unit,
                unitCost,
                notes))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveResource(Guid resourceId)
    {
        EnsureMutableStatus(
            "Resources cannot be removed from a terminal " +
            "cultivation activity.");

        var resource = FindResource(resourceId);

        _resources.Remove(resource);
        UpdatedAt = DateTime.UtcNow;
    }

    private CultivationActivityResource FindResource(
        Guid resourceId)
    {
        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Cultivation activity resource identifier " +
                "cannot be empty.",
                nameof(resourceId));
        }

        return _resources.SingleOrDefault(resource =>
                resource.Id == resourceId)
            ?? throw new KeyNotFoundException(
                $"Cultivation activity resource " +
                $"'{resourceId}' was not found.");
    }

    private (
        SopComplianceStatus Status,
        string? DeviationReason)
        ValidateCompletionCompliance(
            SopComplianceStatus status,
            string? deviationReason)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "SOP compliance status is not supported.");
        }

        if (!IsLinkedToSopStep)
        {
            if (status !=
                SopComplianceStatus.NotApplicable)
            {
                throw new ArgumentException(
                    "An activity without an SOP step must " +
                    "use NotApplicable compliance status.",
                    nameof(status));
            }

            if (!string.IsNullOrWhiteSpace(
                    deviationReason))
            {
                throw new ArgumentException(
                    "Deviation reason cannot be supplied " +
                    "for an activity without an SOP step.",
                    nameof(deviationReason));
            }

            return (
                SopComplianceStatus.NotApplicable,
                null);
        }

        if (status is not
                SopComplianceStatus.Compliant and
            not SopComplianceStatus.Deviated)
        {
            throw new ArgumentException(
                "A completed SOP-linked activity must be " +
                "marked Compliant or Deviated.",
                nameof(status));
        }

        if (status ==
            SopComplianceStatus.Deviated)
        {
            return (
                status,
                NormalizeRequiredText(
                    deviationReason!,
                    MaxDeviationReasonLength,
                    nameof(deviationReason),
                    "Deviation reason"));
        }

        if (!string.IsNullOrWhiteSpace(
                deviationReason))
        {
            throw new ArgumentException(
                "Deviation reason may only be supplied " +
                "when compliance status is Deviated.",
                nameof(deviationReason));
        }

        return (status, null);
    }

    private void EnsureStatus(
        CultivationActivityStatus expectedStatus,
        string message)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void EnsureMutableStatus(string message)
    {
        if (Status is not
                CultivationActivityStatus.Planned and
            not CultivationActivityStatus.InProgress)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static SopSnapshot ValidateSopSnapshot(
        Guid? cultivationSopId,
        Guid? cultivationSopStepId,
        int? sequence,
        string? name,
        int? plannedDayOffset,
        int? estimatedDurationDays,
        bool? isRequired)
    {
        var hasAnyValue =
            cultivationSopId.HasValue ||
            cultivationSopStepId.HasValue ||
            sequence.HasValue ||
            !string.IsNullOrWhiteSpace(name) ||
            plannedDayOffset.HasValue ||
            estimatedDurationDays.HasValue ||
            isRequired.HasValue;

        if (!hasAnyValue)
        {
            return SopSnapshot.Empty;
        }

        var hasAllValues =
            cultivationSopId.HasValue &&
            cultivationSopStepId.HasValue &&
            sequence.HasValue &&
            !string.IsNullOrWhiteSpace(name) &&
            plannedDayOffset.HasValue &&
            estimatedDurationDays.HasValue &&
            isRequired.HasValue;

        if (!hasAllValues)
        {
            throw new ArgumentException(
                "Cultivation SOP step snapshot must be " +
                "provided completely.");
        }

        ValidateIdentifier(
            cultivationSopId!.Value,
            nameof(cultivationSopId),
            "Cultivation SOP");

        ValidateIdentifier(
            cultivationSopStepId!.Value,
            nameof(cultivationSopStepId),
            "Cultivation SOP step");

        if (sequence!.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                "SOP step sequence must be greater than zero.");
        }

        if (estimatedDurationDays!.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimatedDurationDays),
                "SOP estimated duration must be greater " +
                "than zero.");
        }

        return new SopSnapshot(
            cultivationSopId,
            cultivationSopStepId,
            sequence,
            NormalizeRequiredText(
                name!,
                MaxSopStepNameLength,
                nameof(name),
                "SOP step name"),
            plannedDayOffset,
            estimatedDurationDays,
            isRequired);
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

    private static void ValidateActivityType(
        CultivationActivityType activityType)
    {
        if (!Enum.IsDefined(activityType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(activityType),
                activityType,
                "Cultivation activity type is not supported.");
        }
    }

    private static void ValidateDate(
        DateOnly date,
        string parameterName,
        string displayName)
    {
        if (date == default)
        {
            throw new ArgumentException(
                $"{displayName} must be provided.",
                parameterName);
        }
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Cultivation activity code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                "Cultivation activity code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!ActivityCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Cultivation activity code may only contain " +
                "letters, numbers, hyphens, and underscores.",
                nameof(code));
        }

        return normalizedCode;
    }

    private static string NormalizeName(string name)
    {
        return NormalizeRequiredText(
            name,
            MaxNameLength,
            nameof(name),
            "Cultivation activity name");
    }

    private static string NormalizeRequiredText(
        string value,
        int maximumLength,
        string parameterName,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} cannot be empty.",
                parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
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

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private sealed record SopSnapshot(
        Guid? CultivationSopId,
        Guid? CultivationSopStepId,
        int? Sequence,
        string? Name,
        int? PlannedDayOffset,
        int? EstimatedDurationDays,
        bool? IsRequired)
    {
        public static readonly SopSnapshot Empty =
            new(
                null,
                null,
                null,
                null,
                null,
                null,
                null);
    }

    [GeneratedRegex("^[A-Z0-9_-]+$")]
    private static partial Regex ActivityCodePattern();
}
