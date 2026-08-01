using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Domain.Entities.Sales;

public sealed class SaleLine :
    IOrganizationOwned
{
    public const int MaxHarvestBatchCodeLength = 40;

    public const int MaxCropCycleCodeLength = 40;

    public const int MaxCommodityCodeLength = 40;

    public const int MaxCommodityNameLength = 150;

    public const int MaxQualityGradeLength = 100;

    public const int MaxNotesLength = 500;

    private SaleLine()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid SaleId { get; private set; }

    public Guid HarvestBatchId { get; private set; }

    public string HarvestBatchCodeSnapshot
    {
        get;
        private set;
    } = string.Empty;

    public Guid CropCycleIdSnapshot { get; private set; }

    public string CropCycleCodeSnapshot
    {
        get;
        private set;
    } = string.Empty;

    public Guid CommodityIdSnapshot { get; private set; }

    public string CommodityCodeSnapshot
    {
        get;
        private set;
    } = string.Empty;

    public string CommodityNameSnapshot
    {
        get;
        private set;
    } = string.Empty;

    public string? QualityGradeSnapshot
    {
        get;
        private set;
    }

    public decimal Quantity { get; private set; }

    public HarvestQuantityUnit QuantityUnit
    {
        get;
        private set;
    }

    public decimal UnitPrice { get; private set; }

    public decimal LineDiscount { get; private set; }

    public decimal LineTotal { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    internal static SaleLine Create(
        Guid organizationId,
        Guid saleId,
        Guid harvestBatchId,
        string harvestBatchCodeSnapshot,
        Guid cropCycleIdSnapshot,
        string cropCycleCodeSnapshot,
        Guid commodityIdSnapshot,
        string commodityCodeSnapshot,
        string commodityNameSnapshot,
        string? qualityGradeSnapshot,
        decimal quantity,
        HarvestQuantityUnit quantityUnit,
        decimal unitPrice,
        decimal lineDiscount,
        string? notes)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            saleId,
            nameof(saleId),
            "Sale");

        ValidateIdentifier(
            harvestBatchId,
            nameof(harvestBatchId),
            "Harvest batch");

        ValidateIdentifier(
            cropCycleIdSnapshot,
            nameof(cropCycleIdSnapshot),
            "Crop cycle snapshot");

        ValidateIdentifier(
            commodityIdSnapshot,
            nameof(commodityIdSnapshot),
            "Commodity snapshot");

        ValidateQuantityUnit(quantityUnit);

        var amounts = CalculateAmounts(
            quantity,
            unitPrice,
            lineDiscount);

        return new SaleLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SaleId = saleId,
            HarvestBatchId = harvestBatchId,
            HarvestBatchCodeSnapshot =
                NormalizeRequiredText(
                    harvestBatchCodeSnapshot,
                    MaxHarvestBatchCodeLength,
                    nameof(harvestBatchCodeSnapshot),
                    "Harvest batch code snapshot"),
            CropCycleIdSnapshot = cropCycleIdSnapshot,
            CropCycleCodeSnapshot =
                NormalizeRequiredText(
                    cropCycleCodeSnapshot,
                    MaxCropCycleCodeLength,
                    nameof(cropCycleCodeSnapshot),
                    "Crop cycle code snapshot"),
            CommodityIdSnapshot = commodityIdSnapshot,
            CommodityCodeSnapshot =
                NormalizeRequiredText(
                    commodityCodeSnapshot,
                    MaxCommodityCodeLength,
                    nameof(commodityCodeSnapshot),
                    "Commodity code snapshot"),
            CommodityNameSnapshot =
                NormalizeRequiredText(
                    commodityNameSnapshot,
                    MaxCommodityNameLength,
                    nameof(commodityNameSnapshot),
                    "Commodity name snapshot"),
            QualityGradeSnapshot =
                NormalizeOptionalText(
                    qualityGradeSnapshot,
                    MaxQualityGradeLength,
                    nameof(qualityGradeSnapshot)),
            Quantity = amounts.Quantity,
            QuantityUnit = quantityUnit,
            UnitPrice = amounts.UnitPrice,
            LineDiscount = amounts.LineDiscount,
            LineTotal = amounts.LineTotal,
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes)),
            CreatedAt = DateTime.UtcNow
        };
    }

    internal bool Update(
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscount,
        string? notes)
    {
        var amounts = CalculateAmounts(
            quantity,
            unitPrice,
            lineDiscount);

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (Quantity == amounts.Quantity &&
            UnitPrice == amounts.UnitPrice &&
            LineDiscount == amounts.LineDiscount &&
            LineTotal == amounts.LineTotal &&
            Notes == normalizedNotes)
        {
            return false;
        }

        Quantity = amounts.Quantity;
        UnitPrice = amounts.UnitPrice;
        LineDiscount = amounts.LineDiscount;
        LineTotal = amounts.LineTotal;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    internal static SaleLineAmounts CalculateAmounts(
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscount)
    {
        var normalizedQuantity =
            Math.Round(
                quantity,
                4,
                MidpointRounding.AwayFromZero);

        if (normalizedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Sale line quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Sale line unit price cannot be negative.");
        }

        var normalizedUnitPrice =
            Math.Round(
                unitPrice,
                2,
                MidpointRounding.AwayFromZero);

        if (lineDiscount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineDiscount),
                "Sale line discount cannot be negative.");
        }

        var grossLineAmount =
            Math.Round(
                normalizedQuantity * normalizedUnitPrice,
                2,
                MidpointRounding.AwayFromZero);

        var normalizedLineDiscount =
            Math.Round(
                lineDiscount,
                2,
                MidpointRounding.AwayFromZero);

        if (normalizedLineDiscount > grossLineAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineDiscount),
                "Sale line discount cannot exceed " +
                "the gross line amount.");
        }

        var lineTotal =
            Math.Round(
                grossLineAmount - normalizedLineDiscount,
                2,
                MidpointRounding.AwayFromZero);

        return new SaleLineAmounts(
            normalizedQuantity,
            normalizedUnitPrice,
            normalizedLineDiscount,
            lineTotal);
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

internal readonly record struct SaleLineAmounts(
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscount,
    decimal LineTotal);
