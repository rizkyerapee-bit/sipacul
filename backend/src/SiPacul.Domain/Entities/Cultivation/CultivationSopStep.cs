namespace SiPacul.Domain.Entities.Cultivation;

public sealed class CultivationSopStep
{
    public const int MaxNameLength = 150;

    public const int MaxDescriptionLength = 1000;

    public const int MinPlannedDayOffset = -365;

    public const int MaxPlannedDayOffset = 3650;

    public const int MinEstimatedDurationDays = 1;

    public const int MaxEstimatedDurationDays = 365;

    private CultivationSopStep()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid CultivationSopId { get; private set; }

    public int Sequence { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int PlannedDayOffset { get; private set; }

    public int EstimatedDurationDays { get; private set; }

    public bool IsRequired { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    internal static CultivationSopStep Create(
        Guid organizationId,
        Guid cultivationSopId,
        int sequence,
        string name,
        string? description,
        int plannedDayOffset,
        int estimatedDurationDays,
        bool isRequired)
    {
        ValidateOrganizationId(organizationId);
        ValidateCultivationSopId(cultivationSopId);
        ValidateSequence(sequence);
        ValidateName(name);
        ValidateDescription(description);
        ValidatePlannedDayOffset(plannedDayOffset);
        ValidateEstimatedDurationDays(
            estimatedDurationDays);

        return new CultivationSopStep
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CultivationSopId = cultivationSopId,
            Sequence = sequence,
            Name = name.Trim(),
            Description = NormalizeOptionalText(
                description),
            PlannedDayOffset = plannedDayOffset,
            EstimatedDurationDays =
                estimatedDurationDays,
            IsRequired = isRequired,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal bool Update(
        string name,
        string? description,
        int plannedDayOffset,
        int estimatedDurationDays,
        bool isRequired)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidatePlannedDayOffset(plannedDayOffset);
        ValidateEstimatedDurationDays(
            estimatedDurationDays);

        var normalizedName = name.Trim();

        var normalizedDescription =
            NormalizeOptionalText(description);

        if (Name == normalizedName &&
            Description == normalizedDescription &&
            PlannedDayOffset == plannedDayOffset &&
            EstimatedDurationDays ==
                estimatedDurationDays &&
            IsRequired == isRequired)
        {
            return false;
        }

        Name = normalizedName;
        Description = normalizedDescription;
        PlannedDayOffset = plannedDayOffset;
        EstimatedDurationDays =
            estimatedDurationDays;
        IsRequired = isRequired;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    internal bool ChangeSequence(int sequence)
    {
        ValidateSequence(sequence);

        if (Sequence == sequence)
        {
            return false;
        }

        Sequence = sequence;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    private static void ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty.",
                nameof(organizationId));
        }
    }

    private static void ValidateCultivationSopId(
        Guid cultivationSopId)
    {
        if (cultivationSopId == Guid.Empty)
        {
            throw new ArgumentException(
                "Cultivation SOP identifier cannot be empty.",
                nameof(cultivationSopId));
        }
    }

    private static void ValidateSequence(int sequence)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                "Step sequence must be greater than zero.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "SOP step name cannot be empty.",
                nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"SOP step name cannot exceed " +
                $"{MaxNameLength} characters.",
                nameof(name));
        }
    }

    private static void ValidateDescription(
        string? description)
    {
        if (description?.Trim().Length >
            MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"SOP step description cannot exceed " +
                $"{MaxDescriptionLength} characters.",
                nameof(description));
        }
    }

    private static void ValidatePlannedDayOffset(
        int plannedDayOffset)
    {
        if (plannedDayOffset <
                MinPlannedDayOffset ||
            plannedDayOffset >
                MaxPlannedDayOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plannedDayOffset),
                $"Planned day offset must be between " +
                $"{MinPlannedDayOffset} and " +
                $"{MaxPlannedDayOffset}.");
        }
    }

    private static void ValidateEstimatedDurationDays(
        int estimatedDurationDays)
    {
        if (estimatedDurationDays <
                MinEstimatedDurationDays ||
            estimatedDurationDays >
                MaxEstimatedDurationDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimatedDurationDays),
                $"Estimated duration must be between " +
                $"{MinEstimatedDurationDays} and " +
                $"{MaxEstimatedDurationDays} days.");
        }
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
