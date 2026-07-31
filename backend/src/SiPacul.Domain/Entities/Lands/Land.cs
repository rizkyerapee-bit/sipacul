using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Lands;

public sealed partial class Land :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 30;

    public const int MaxNameLength = 150;

    public const int MaxAddressLength = 500;

    public const int MaxLocationDescriptionLength = 500;

    public const int MaxNotesLength = 1000;

    public const decimal MinLatitude = -90m;

    public const decimal MaxLatitude = 90m;

    public const decimal MinLongitude = -180m;

    public const decimal MaxLongitude = 180m;

    private readonly List<LandPlot> _plots = [];

    private Land()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public LandTenureType TenureType { get; private set; }

    public decimal TotalArea { get; private set; }

    public AreaUnit AreaUnit { get; private set; }

    public string? Address { get; private set; }

    public string? LocationDescription { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<LandPlot> Plots =>
        _plots.AsReadOnly();

    public decimal TotalAreaInSquareMeters =>
        AreaUnitConverter.ToSquareMeters(
            TotalArea,
            AreaUnit);

    public decimal AllocatedPlotAreaInSquareMeters =>
        _plots.Sum(
            plot => plot.GetAreaInSquareMeters());

    public static Land Create(
        Guid organizationId,
        string code,
        string name,
        LandTenureType tenureType,
        decimal totalArea,
        AreaUnit areaUnit,
        string? address,
        string? locationDescription,
        decimal? latitude,
        decimal? longitude,
        string? notes)
    {
        ValidateOrganizationId(organizationId);
        ValidateTenureType(tenureType);
        ValidateArea(totalArea, areaUnit);

        var coordinates = ValidateCoordinates(
            latitude,
            longitude);

        return new Land
        {
            OrganizationId = organizationId,
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            TenureType = tenureType,
            TotalArea = totalArea,
            AreaUnit = areaUnit,
            Address = NormalizeOptionalText(
                address,
                MaxAddressLength,
                nameof(address)),
            LocationDescription =
                NormalizeOptionalText(
                    locationDescription,
                    MaxLocationDescriptionLength,
                    nameof(locationDescription)),
            Latitude = coordinates.Latitude,
            Longitude = coordinates.Longitude,
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes))
        };
    }

    public void Update(
        string name,
        LandTenureType tenureType,
        decimal totalArea,
        AreaUnit areaUnit,
        string? address,
        string? locationDescription,
        decimal? latitude,
        decimal? longitude,
        string? notes)
    {
        ValidateTenureType(tenureType);
        ValidateArea(totalArea, areaUnit);

        var normalizedName = NormalizeName(name);

        var normalizedAddress =
            NormalizeOptionalText(
                address,
                MaxAddressLength,
                nameof(address));

        var normalizedLocationDescription =
            NormalizeOptionalText(
                locationDescription,
                MaxLocationDescriptionLength,
                nameof(locationDescription));

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        var coordinates = ValidateCoordinates(
            latitude,
            longitude);

        EnsureAllocatedAreaFits(
            totalArea,
            areaUnit,
            null,
            null);

        if (Name == normalizedName &&
            TenureType == tenureType &&
            TotalArea == totalArea &&
            AreaUnit == areaUnit &&
            Address == normalizedAddress &&
            LocationDescription ==
                normalizedLocationDescription &&
            Latitude == coordinates.Latitude &&
            Longitude == coordinates.Longitude &&
            Notes == normalizedNotes)
        {
            return;
        }

        Name = normalizedName;
        TenureType = tenureType;
        TotalArea = totalArea;
        AreaUnit = areaUnit;
        Address = normalizedAddress;
        LocationDescription =
            normalizedLocationDescription;
        Latitude = coordinates.Latitude;
        Longitude = coordinates.Longitude;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public LandPlot AddPlot(
        string code,
        string name,
        decimal area,
        AreaUnit areaUnit,
        string? generalCondition,
        string? notes)
    {
        var normalizedCode =
            NormalizePlotCodeForComparison(code);

        if (_plots.Any(
            plot => plot.Code == normalizedCode))
        {
            throw new InvalidOperationException(
                $"Land plot code '{normalizedCode}' " +
                "already exists in this land.");
        }

        EnsureAllocatedAreaFits(
            TotalArea,
            AreaUnit,
            area,
            areaUnit);

        var plot = LandPlot.Create(
            OrganizationId,
            Id,
            code,
            name,
            area,
            areaUnit,
            generalCondition,
            notes);

        _plots.Add(plot);
        UpdatedAt = DateTime.UtcNow;

        return plot;
    }

    public void UpdatePlot(
        Guid plotId,
        string name,
        decimal area,
        AreaUnit areaUnit,
        string? generalCondition,
        string? notes)
    {
        var plot = FindPlot(plotId);

        EnsureAllocatedAreaFits(
            TotalArea,
            AreaUnit,
            area,
            areaUnit,
            plot.Id);

        var hasChanged = plot.Update(
            name,
            area,
            areaUnit,
            generalCondition,
            notes);

        if (hasChanged)
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemovePlot(Guid plotId)
    {
        var plot = FindPlot(plotId);

        _plots.Remove(plot);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ActivatePlot(Guid plotId)
    {
        var plot = FindPlot(plotId);

        if (plot.Activate())
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void DeactivatePlot(Guid plotId)
    {
        var plot = FindPlot(plotId);

        if (plot.Deactivate())
        {
            UpdatedAt = DateTime.UtcNow;
        }
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

    private LandPlot FindPlot(Guid plotId)
    {
        if (plotId == Guid.Empty)
        {
            throw new ArgumentException(
                "Land plot identifier cannot be empty.",
                nameof(plotId));
        }

        return _plots.SingleOrDefault(
                plot => plot.Id == plotId)
            ?? throw new KeyNotFoundException(
                $"Land plot '{plotId}' was not found.");
    }

    private void EnsureAllocatedAreaFits(
        decimal landArea,
        AreaUnit landAreaUnit,
        decimal? candidatePlotArea,
        AreaUnit? candidatePlotAreaUnit,
        Guid? excludedPlotId = null)
    {
        var allocatedArea =
            _plots
                .Where(plot =>
                    excludedPlotId is null ||
                    plot.Id != excludedPlotId.Value)
                .Sum(plot =>
                    plot.GetAreaInSquareMeters());

        if (candidatePlotArea is not null)
        {
            if (candidatePlotAreaUnit is null)
            {
                throw new ArgumentNullException(
                    nameof(candidatePlotAreaUnit));
            }

            ValidateArea(
                candidatePlotArea.Value,
                candidatePlotAreaUnit.Value);

            allocatedArea +=
                AreaUnitConverter.ToSquareMeters(
                    candidatePlotArea.Value,
                    candidatePlotAreaUnit.Value);
        }

        var availableLandArea =
            AreaUnitConverter.ToSquareMeters(
                landArea,
                landAreaUnit);

        if (allocatedArea > availableLandArea)
        {
            throw new InvalidOperationException(
                "Total land plot area cannot exceed " +
                "the total land area.");
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

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Land code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                $"Land code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!LandCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Land code may only contain letters, " +
                "numbers, hyphens, and underscores.",
                nameof(code));
        }

        return normalizedCode;
    }

    private static string NormalizePlotCodeForComparison(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Land plot code cannot be empty.",
                nameof(code));
        }

        return code.Trim().ToUpperInvariant();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Land name cannot be empty.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Land name cannot exceed " +
                $"{MaxNameLength} characters.",
                nameof(name));
        }

        return normalizedName;
    }

    private static void ValidateTenureType(
        LandTenureType tenureType)
    {
        if (!Enum.IsDefined(tenureType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tenureType),
                tenureType,
                "Land tenure type is not supported.");
        }
    }

    private static void ValidateArea(
        decimal area,
        AreaUnit areaUnit)
    {
        if (area <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(area),
                "Land area must be greater than zero.");
        }

        _ = AreaUnitConverter.ToSquareMeters(
            area,
            areaUnit);
    }

    private static (
        decimal? Latitude,
        decimal? Longitude) ValidateCoordinates(
            decimal? latitude,
            decimal? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException(
                "Latitude and longitude must be " +
                "provided together.",
                nameof(latitude));
        }

        if (!latitude.HasValue)
        {
            return (null, null);
        }

        if (latitude.Value < MinLatitude ||
            latitude.Value > MaxLatitude)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                $"Latitude must be between " +
                $"{MinLatitude} and {MaxLatitude}.");
        }

        if (longitude!.Value < MinLongitude ||
            longitude.Value > MaxLongitude)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                $"Longitude must be between " +
                $"{MinLongitude} and {MaxLongitude}.");
        }

        return (latitude, longitude);
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
    private static partial Regex LandCodePattern();
}
