using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Domain.Entities.Cultivation;

public sealed partial class CropCycle :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxNameLength = 150;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private CropCycle()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid CommodityId { get; private set; }

    public Guid? CultivationSopId { get; private set; }

    public Guid LandId { get; private set; }

    public Guid LandPlotId { get; private set; }

    public decimal PlantedArea { get; private set; }

    public AreaUnit AreaUnit { get; private set; }

    public DateOnly PlannedStartDate { get; private set; }

    public DateOnly ExpectedHarvestDate { get; private set; }

    public DateOnly? ActualStartDate { get; private set; }

    public DateOnly? ActualHarvestDate { get; private set; }

    public CropCycleStatus Status { get; private set; } =
        CropCycleStatus.Planned;

    public string? CancellationReason { get; private set; }

    public string? Notes { get; private set; }

    public decimal PlantedAreaInSquareMeters =>
        AreaUnitConverter.ToSquareMeters(
            PlantedArea,
            AreaUnit);

    public static CropCycle Create(
        Guid organizationId,
        string code,
        string name,
        Guid commodityId,
        Guid? cultivationSopId,
        Guid landId,
        Guid landPlotId,
        decimal plantedArea,
        AreaUnit areaUnit,
        DateOnly plannedStartDate,
        DateOnly expectedHarvestDate,
        string? notes)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            commodityId,
            nameof(commodityId),
            "Commodity");

        ValidateOptionalIdentifier(
            cultivationSopId,
            nameof(cultivationSopId),
            "Cultivation SOP");

        ValidateIdentifier(
            landId,
            nameof(landId),
            "Land");

        ValidateIdentifier(
            landPlotId,
            nameof(landPlotId),
            "Land plot");

        ValidateArea(
            plantedArea,
            areaUnit);

        ValidatePlannedDates(
            plannedStartDate,
            expectedHarvestDate);

        return new CropCycle
        {
            OrganizationId = organizationId,
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            CommodityId = commodityId,
            CultivationSopId = cultivationSopId,
            LandId = landId,
            LandPlotId = landPlotId,
            PlantedArea = plantedArea,
            AreaUnit = areaUnit,
            PlannedStartDate = plannedStartDate,
            ExpectedHarvestDate = expectedHarvestDate,
            Status = CropCycleStatus.Planned,
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes))
        };
    }

    public void UpdatePlan(
        string name,
        Guid? cultivationSopId,
        decimal plantedArea,
        AreaUnit areaUnit,
        DateOnly plannedStartDate,
        DateOnly expectedHarvestDate,
        string? notes)
    {
        EnsureStatus(
            CropCycleStatus.Planned,
            "Only a planned crop cycle can be updated.");

        ValidateOptionalIdentifier(
            cultivationSopId,
            nameof(cultivationSopId),
            "Cultivation SOP");

        ValidateArea(
            plantedArea,
            areaUnit);

        ValidatePlannedDates(
            plannedStartDate,
            expectedHarvestDate);

        var normalizedName = NormalizeName(name);

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (Name == normalizedName &&
            CultivationSopId == cultivationSopId &&
            PlantedArea == plantedArea &&
            AreaUnit == areaUnit &&
            PlannedStartDate == plannedStartDate &&
            ExpectedHarvestDate ==
                expectedHarvestDate &&
            Notes == normalizedNotes)
        {
            return;
        }

        Name = normalizedName;
        CultivationSopId = cultivationSopId;
        PlantedArea = plantedArea;
        AreaUnit = areaUnit;
        PlannedStartDate = plannedStartDate;
        ExpectedHarvestDate = expectedHarvestDate;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start(DateOnly actualStartDate)
    {
        EnsureStatus(
            CropCycleStatus.Planned,
            "Only a planned crop cycle can be started.");

        if (actualStartDate > ExpectedHarvestDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualStartDate),
                "Actual start date cannot be after " +
                "the expected harvest date.");
        }

        ActualStartDate = actualStartDate;
        Status = CropCycleStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(DateOnly actualHarvestDate)
    {
        EnsureStatus(
            CropCycleStatus.InProgress,
            "Only an in-progress crop cycle can be completed.");

        if (!ActualStartDate.HasValue)
        {
            throw new InvalidOperationException(
                "An in-progress crop cycle must have " +
                "an actual start date.");
        }

        if (actualHarvestDate < ActualStartDate.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualHarvestDate),
                "Actual harvest date cannot be before " +
                "the actual start date.");
        }

        ActualHarvestDate = actualHarvestDate;
        Status = CropCycleStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        if (Status is not CropCycleStatus.Planned and
            not CropCycleStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Only a planned or in-progress crop cycle " +
                "can be cancelled.");
        }

        var normalizedReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        CancellationReason = normalizedReason;
        Status = CropCycleStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        if (Status is not CropCycleStatus.Planned and
            not CropCycleStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Notes cannot be updated on a terminal " +
                "crop cycle.");
        }

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (Notes == normalizedNotes)
        {
            return;
        }

        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(
        CropCycleStatus expectedStatus,
        string message)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(message);
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

    private static void ValidateOptionalIdentifier(
        Guid? identifier,
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

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Crop cycle code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                $"Crop cycle code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!CropCycleCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Crop cycle code may only contain letters, " +
                "numbers, hyphens, and underscores.",
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
            "Crop cycle name");
    }

    private static void ValidateArea(
        decimal plantedArea,
        AreaUnit areaUnit)
    {
        if (plantedArea <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plantedArea),
                "Planted area must be greater than zero.");
        }

        _ = AreaUnitConverter.ToSquareMeters(
            plantedArea,
            areaUnit);
    }

    private static void ValidatePlannedDates(
        DateOnly plannedStartDate,
        DateOnly expectedHarvestDate)
    {
        if (expectedHarvestDate <= plannedStartDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedHarvestDate),
                "Expected harvest date must be after " +
                "the planned start date.");
        }
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

    [GeneratedRegex("^[A-Z0-9_-]+$")]
    private static partial Regex CropCycleCodePattern();
}
