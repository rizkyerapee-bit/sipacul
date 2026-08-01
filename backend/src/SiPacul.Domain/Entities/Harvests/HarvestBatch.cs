using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Harvests;

public sealed partial class HarvestBatch :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxQualityGradeLength = 100;

    public const int MaxStorageLocationLength = 250;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private HarvestBatch()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public DateOnly HarvestDate { get; private set; }

    public decimal GrossQuantity { get; private set; }

    public decimal RejectedQuantity { get; private set; }

    public decimal NetQuantity { get; private set; }

    public HarvestQuantityUnit QuantityUnit
    {
        get;
        private set;
    }

    public string? QualityGrade { get; private set; }

    public string? StorageLocation { get; private set; }

    public string? Notes { get; private set; }

    public HarvestBatchStatus Status { get; private set; } =
        HarvestBatchStatus.Draft;

    public DateTime? ConfirmedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public bool IsSellable =>
        Status == HarvestBatchStatus.Confirmed;

    public static HarvestBatch Create(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        DateOnly harvestDate,
        decimal grossQuantity,
        decimal rejectedQuantity,
        HarvestQuantityUnit quantityUnit,
        string? qualityGrade,
        string? storageLocation,
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

        ValidateDate(
            harvestDate,
            nameof(harvestDate),
            "Harvest date");

        ValidateQuantityUnit(quantityUnit);

        var quantities =
            NormalizeQuantities(
                grossQuantity,
                rejectedQuantity);

        return new HarvestBatch
        {
            OrganizationId = organizationId,
            CropCycleId = cropCycleId,
            Code = NormalizeCode(code),
            HarvestDate = harvestDate,
            GrossQuantity = quantities.Gross,
            RejectedQuantity = quantities.Rejected,
            NetQuantity = quantities.Net,
            QuantityUnit = quantityUnit,
            QualityGrade = NormalizeOptionalText(
                qualityGrade,
                MaxQualityGradeLength,
                nameof(qualityGrade)),
            StorageLocation = NormalizeOptionalText(
                storageLocation,
                MaxStorageLocationLength,
                nameof(storageLocation)),
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes)),
            Status = HarvestBatchStatus.Draft
        };
    }

    public void UpdateDraft(
        DateOnly harvestDate,
        decimal grossQuantity,
        decimal rejectedQuantity,
        HarvestQuantityUnit quantityUnit,
        string? qualityGrade,
        string? storageLocation,
        string? notes)
    {
        EnsureStatus(
            HarvestBatchStatus.Draft,
            "Only a draft harvest batch can be updated.");

        ValidateDate(
            harvestDate,
            nameof(harvestDate),
            "Harvest date");

        ValidateQuantityUnit(quantityUnit);

        var quantities =
            NormalizeQuantities(
                grossQuantity,
                rejectedQuantity);

        var normalizedQuality =
            NormalizeOptionalText(
                qualityGrade,
                MaxQualityGradeLength,
                nameof(qualityGrade));

        var normalizedStorage =
            NormalizeOptionalText(
                storageLocation,
                MaxStorageLocationLength,
                nameof(storageLocation));

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (HarvestDate == harvestDate &&
            GrossQuantity == quantities.Gross &&
            RejectedQuantity == quantities.Rejected &&
            NetQuantity == quantities.Net &&
            QuantityUnit == quantityUnit &&
            QualityGrade == normalizedQuality &&
            StorageLocation == normalizedStorage &&
            Notes == normalizedNotes)
        {
            return;
        }

        HarvestDate = harvestDate;
        GrossQuantity = quantities.Gross;
        RejectedQuantity = quantities.Rejected;
        NetQuantity = quantities.Net;
        QuantityUnit = quantityUnit;
        QualityGrade = normalizedQuality;
        StorageLocation = normalizedStorage;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        EnsureStatus(
            HarvestBatchStatus.Draft,
            "Only a draft harvest batch can be confirmed.");

        if (NetQuantity <= 0)
        {
            throw new InvalidOperationException(
                "A harvest batch must have a positive " +
                "net quantity before confirmation.");
        }

        Status = HarvestBatchStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        if (Status is not HarvestBatchStatus.Draft and
            not HarvestBatchStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only a draft or confirmed harvest batch " +
                "can be cancelled.");
        }

        CancellationReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        Status = HarvestBatchStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(
        HarvestBatchStatus expectedStatus,
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

    private static void ValidateQuantityUnit(
        HarvestQuantityUnit quantityUnit)
    {
        if (!Enum.IsDefined(quantityUnit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityUnit),
                quantityUnit,
                "Harvest quantity unit is not supported.");
        }
    }

    private static (
        decimal Gross,
        decimal Rejected,
        decimal Net)
        NormalizeQuantities(
            decimal grossQuantity,
            decimal rejectedQuantity)
    {
        var normalizedGross =
            Math.Round(
                grossQuantity,
                4,
                MidpointRounding.AwayFromZero);

        if (normalizedGross <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(grossQuantity),
                "Gross quantity must be greater than zero.");
        }

        var normalizedRejected =
            Math.Round(
                rejectedQuantity,
                4,
                MidpointRounding.AwayFromZero);

        if (normalizedRejected < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rejectedQuantity),
                "Rejected quantity cannot be negative.");
        }

        if (normalizedRejected > normalizedGross)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rejectedQuantity),
                "Rejected quantity cannot exceed " +
                "gross quantity.");
        }

        var normalizedNet =
            Math.Round(
                normalizedGross - normalizedRejected,
                4,
                MidpointRounding.AwayFromZero);

        return (
            normalizedGross,
            normalizedRejected,
            normalizedNet);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Harvest batch code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                "Harvest batch code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!HarvestBatchCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Harvest batch code may only contain " +
                "letters, numbers, periods, hyphens, " +
                "and underscores, and must start with " +
                "a letter or number.",
                nameof(code));
        }

        return normalizedCode;
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

    [GeneratedRegex(
        "^[A-Z0-9][A-Z0-9._-]{0,39}$")]
    private static partial Regex
        HarvestBatchCodePattern();
}
