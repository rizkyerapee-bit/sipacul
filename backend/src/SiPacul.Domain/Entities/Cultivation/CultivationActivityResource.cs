using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Cultivation;

public sealed class CultivationActivityResource :
    IOrganizationOwned
{
    public const int MaxDescriptionLength = 250;

    public const int MaxUnitLength = 50;

    public const int MaxNotesLength = 500;

    private CultivationActivityResource()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid CultivationActivityId { get; private set; }

    public CultivationResourceType ResourceType
    {
        get;
        private set;
    }

    public string Description { get; private set; } =
        string.Empty;

    public decimal Quantity { get; private set; }

    public string Unit { get; private set; } =
        string.Empty;

    public decimal UnitCost { get; private set; }

    public decimal TotalCost { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    internal static CultivationActivityResource Create(
        Guid organizationId,
        Guid cultivationActivityId,
        CultivationResourceType resourceType,
        string description,
        decimal quantity,
        string unit,
        decimal unitCost,
        string? notes)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            cultivationActivityId,
            nameof(cultivationActivityId),
            "Cultivation activity");

        ValidateResourceType(resourceType);

        var normalizedQuantity =
            NormalizeQuantity(quantity);

        var normalizedUnitCost =
            NormalizeUnitCost(unitCost);

        return new CultivationActivityResource
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CultivationActivityId =
                cultivationActivityId,
            ResourceType = resourceType,
            Description = NormalizeRequiredText(
                description,
                MaxDescriptionLength,
                nameof(description),
                "Resource description"),
            Quantity = normalizedQuantity,
            Unit = NormalizeRequiredText(
                unit,
                MaxUnitLength,
                nameof(unit),
                "Resource unit"),
            UnitCost = normalizedUnitCost,
            TotalCost = CalculateTotalCost(
                normalizedQuantity,
                normalizedUnitCost),
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes)),
            CreatedAt = DateTime.UtcNow
        };
    }

    internal bool Update(
        string description,
        decimal quantity,
        string unit,
        decimal unitCost,
        string? notes)
    {
        var normalizedDescription =
            NormalizeRequiredText(
                description,
                MaxDescriptionLength,
                nameof(description),
                "Resource description");

        var normalizedQuantity =
            NormalizeQuantity(quantity);

        var normalizedUnit =
            NormalizeRequiredText(
                unit,
                MaxUnitLength,
                nameof(unit),
                "Resource unit");

        var normalizedUnitCost =
            NormalizeUnitCost(unitCost);

        var normalizedTotalCost =
            CalculateTotalCost(
                normalizedQuantity,
                normalizedUnitCost);

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (Description == normalizedDescription &&
            Quantity == normalizedQuantity &&
            Unit == normalizedUnit &&
            UnitCost == normalizedUnitCost &&
            TotalCost == normalizedTotalCost &&
            Notes == normalizedNotes)
        {
            return false;
        }

        Description = normalizedDescription;
        Quantity = normalizedQuantity;
        Unit = normalizedUnit;
        UnitCost = normalizedUnitCost;
        TotalCost = normalizedTotalCost;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;

        return true;
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

    private static void ValidateResourceType(
        CultivationResourceType resourceType)
    {
        if (!Enum.IsDefined(resourceType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(resourceType),
                resourceType,
                "Cultivation resource type is not supported.");
        }
    }

    private static decimal NormalizeQuantity(
        decimal quantity)
    {
        var normalizedQuantity = Math.Round(
            quantity,
            4,
            MidpointRounding.AwayFromZero);

        if (normalizedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Resource quantity must be greater than zero.");
        }

        return normalizedQuantity;
    }

    private static decimal NormalizeUnitCost(
        decimal unitCost)
    {
        if (unitCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitCost),
                "Resource unit cost cannot be negative.");
        }

        return Math.Round(
            unitCost,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateTotalCost(
        decimal quantity,
        decimal unitCost)
    {
        return Math.Round(
            quantity * unitCost,
            2,
            MidpointRounding.AwayFromZero);
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
}
