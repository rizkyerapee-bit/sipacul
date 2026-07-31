using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Lands;

public sealed partial class LandPlot :
    IOrganizationOwned
{
    public const int MaxCodeLength = 30;

    public const int MaxNameLength = 150;

    public const int MaxGeneralConditionLength = 500;

    public const int MaxNotesLength = 1000;

    private LandPlot()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid LandId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public decimal Area { get; private set; }

    public AreaUnit AreaUnit { get; private set; }

    public string? GeneralCondition { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    internal static LandPlot Create(
        Guid organizationId,
        Guid landId,
        string code,
        string name,
        decimal area,
        AreaUnit areaUnit,
        string? generalCondition,
        string? notes)
    {
        ValidateOrganizationId(organizationId);
        ValidateLandId(landId);
        ValidateArea(area, areaUnit);

        return new LandPlot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LandId = landId,
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            Area = area,
            AreaUnit = areaUnit,
            GeneralCondition = NormalizeOptionalText(
                generalCondition,
                MaxGeneralConditionLength,
                nameof(generalCondition)),
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes)),
            CreatedAt = DateTime.UtcNow
        };
    }

    internal bool Update(
        string name,
        decimal area,
        AreaUnit areaUnit,
        string? generalCondition,
        string? notes)
    {
        ValidateArea(area, areaUnit);

        var normalizedName = NormalizeName(name);

        var normalizedGeneralCondition =
            NormalizeOptionalText(
                generalCondition,
                MaxGeneralConditionLength,
                nameof(generalCondition));

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (Name == normalizedName &&
            Area == area &&
            AreaUnit == areaUnit &&
            GeneralCondition ==
                normalizedGeneralCondition &&
            Notes == normalizedNotes)
        {
            return false;
        }

        Name = normalizedName;
        Area = area;
        AreaUnit = areaUnit;
        GeneralCondition =
            normalizedGeneralCondition;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    internal bool Activate()
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    internal bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    internal decimal GetAreaInSquareMeters()
    {
        return AreaUnitConverter.ToSquareMeters(
            Area,
            AreaUnit);
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

    private static void ValidateLandId(Guid landId)
    {
        if (landId == Guid.Empty)
        {
            throw new ArgumentException(
                "Land identifier cannot be empty.",
                nameof(landId));
        }
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Land plot code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                $"Land plot code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!LandPlotCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Land plot code may only contain " +
                "letters, numbers, hyphens, and underscores.",
                nameof(code));
        }

        return normalizedCode;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Land plot name cannot be empty.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Land plot name cannot exceed " +
                $"{MaxNameLength} characters.",
                nameof(name));
        }

        return normalizedName;
    }

    private static void ValidateArea(
        decimal area,
        AreaUnit areaUnit)
    {
        if (area <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(area),
                "Land plot area must be greater than zero.");
        }

        _ = AreaUnitConverter.ToSquareMeters(
            area,
            areaUnit);
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
    private static partial Regex LandPlotCodePattern();
}
