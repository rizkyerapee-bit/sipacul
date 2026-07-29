using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Cultivation;

public sealed class CultivationSop :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxNameLength = 150;

    public const int MaxDescriptionLength = 1000;

    private readonly List<CultivationSopStep> _steps = [];

    private CultivationSop()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CommodityId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<CultivationSopStep> Steps =>
        _steps.AsReadOnly();

    public static CultivationSop Create(
        Guid organizationId,
        Guid commodityId,
        string name,
        string? description)
    {
        ValidateOrganizationId(organizationId);
        ValidateCommodityId(commodityId);
        ValidateName(name);
        ValidateDescription(description);

        return new CultivationSop
        {
            OrganizationId = organizationId,
            CommodityId = commodityId,
            Name = name.Trim(),
            Description = NormalizeOptionalText(
                description)
        };
    }

    public void Update(
        string name,
        string? description)
    {
        ValidateName(name);
        ValidateDescription(description);

        var normalizedName = name.Trim();

        var normalizedDescription =
            NormalizeOptionalText(description);

        if (Name == normalizedName &&
            Description == normalizedDescription)
        {
            return;
        }

        Name = normalizedName;
        Description = normalizedDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    public CultivationSopStep AddStep(
        string name,
        string? description,
        int plannedDayOffset,
        int estimatedDurationDays,
        bool isRequired)
    {
        var step = CultivationSopStep.Create(
            OrganizationId,
            Id,
            _steps.Count + 1,
            name,
            description,
            plannedDayOffset,
            estimatedDurationDays,
            isRequired);

        _steps.Add(step);

        UpdatedAt = DateTime.UtcNow;

        return step;
    }

    public void UpdateStep(
        Guid stepId,
        string name,
        string? description,
        int plannedDayOffset,
        int estimatedDurationDays,
        bool isRequired)
    {
        var step = FindStep(stepId);

        var hasChanged = step.Update(
            name,
            description,
            plannedDayOffset,
            estimatedDurationDays,
            isRequired);

        if (hasChanged)
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveStep(Guid stepId)
    {
        var step = FindStep(stepId);

        _steps.Remove(step);

        ResequenceSteps();

        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveStep(
        Guid stepId,
        int newSequence)
    {
        ValidateStepSequenceForMove(newSequence);

        var currentIndex = _steps.FindIndex(
            step => step.Id == stepId);

        if (currentIndex < 0)
        {
            throw new KeyNotFoundException(
                $"Cultivation SOP step '{stepId}' " +
                "was not found.");
        }

        var targetIndex = newSequence - 1;

        if (currentIndex == targetIndex)
        {
            return;
        }

        var step = _steps[currentIndex];

        _steps.RemoveAt(currentIndex);
        _steps.Insert(targetIndex, step);

        ResequenceSteps();

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private CultivationSopStep FindStep(
        Guid stepId)
    {
        if (stepId == Guid.Empty)
        {
            throw new ArgumentException(
                "Cultivation SOP step identifier " +
                "cannot be empty.",
                nameof(stepId));
        }

        return _steps.SingleOrDefault(
                step => step.Id == stepId)
            ?? throw new KeyNotFoundException(
                $"Cultivation SOP step '{stepId}' " +
                "was not found.");
    }

    private void ResequenceSteps()
    {
        for (
            var index = 0;
            index < _steps.Count;
            index++)
        {
            _steps[index].ChangeSequence(
                index + 1);
        }
    }

    private void ValidateStepSequenceForMove(
        int sequence)
    {
        if (sequence <= 0 ||
            sequence > _steps.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                $"Step sequence must be between 1 " +
                $"and {_steps.Count}.");
        }
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

    private static void ValidateCommodityId(
        Guid commodityId)
    {
        if (commodityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Commodity identifier cannot be empty.",
                nameof(commodityId));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Cultivation SOP name cannot be empty.",
                nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Cultivation SOP name cannot exceed " +
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
                $"Cultivation SOP description cannot exceed " +
                $"{MaxDescriptionLength} characters.",
                nameof(description));
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
