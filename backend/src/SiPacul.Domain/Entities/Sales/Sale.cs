using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Domain.Entities.Sales;

public sealed partial class Sale :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxBuyerNameLength = 150;

    public const int MaxBuyerPhoneLength = 50;

    public const int MaxBuyerAddressLength = 500;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private readonly List<SaleLine> _lines = [];

    private Sale()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public DateOnly SaleDate { get; private set; }

    public string BuyerName { get; private set; } =
        string.Empty;

    public string? BuyerPhone { get; private set; }

    public string? BuyerAddress { get; private set; }

    public SalePaymentTerm PaymentTerm
    {
        get;
        private set;
    }

    public DateOnly? DueDate { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TotalAmount { get; private set; }

    public SaleStatus Status { get; private set; } =
        SaleStatus.Draft;

    public DateTime? ConfirmedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<SaleLine> Lines =>
        _lines.AsReadOnly();

    public bool IsRevenue =>
        Status == SaleStatus.Confirmed;

    public static Sale Create(
        Guid organizationId,
        string code,
        DateOnly saleDate,
        string buyerName,
        string? buyerPhone,
        string? buyerAddress,
        SalePaymentTerm paymentTerm,
        DateOnly? dueDate,
        decimal discountAmount,
        string? notes)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateDate(
            saleDate,
            nameof(saleDate),
            "Sale date");

        ValidatePaymentTerms(
            saleDate,
            paymentTerm,
            dueDate);

        var normalizedDiscount =
            NormalizeDiscount(
                discountAmount,
                0);

        return new Sale
        {
            OrganizationId = organizationId,
            Code = NormalizeCode(code),
            SaleDate = saleDate,
            BuyerName = NormalizeRequiredText(
                buyerName,
                MaxBuyerNameLength,
                nameof(buyerName),
                "Buyer name"),
            BuyerPhone = NormalizeOptionalText(
                buyerPhone,
                MaxBuyerPhoneLength,
                nameof(buyerPhone)),
            BuyerAddress = NormalizeOptionalText(
                buyerAddress,
                MaxBuyerAddressLength,
                nameof(buyerAddress)),
            PaymentTerm = paymentTerm,
            DueDate = dueDate,
            DiscountAmount = normalizedDiscount,
            Subtotal = 0,
            TotalAmount = 0,
            Status = SaleStatus.Draft,
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes))
        };
    }

    public void UpdateDraft(
        DateOnly saleDate,
        string buyerName,
        string? buyerPhone,
        string? buyerAddress,
        SalePaymentTerm paymentTerm,
        DateOnly? dueDate,
        decimal discountAmount,
        string? notes)
    {
        EnsureDraft(
            "Only a draft sale can be updated.");

        ValidateDate(
            saleDate,
            nameof(saleDate),
            "Sale date");

        ValidatePaymentTerms(
            saleDate,
            paymentTerm,
            dueDate);

        var normalizedBuyerName =
            NormalizeRequiredText(
                buyerName,
                MaxBuyerNameLength,
                nameof(buyerName),
                "Buyer name");

        var normalizedBuyerPhone =
            NormalizeOptionalText(
                buyerPhone,
                MaxBuyerPhoneLength,
                nameof(buyerPhone));

        var normalizedBuyerAddress =
            NormalizeOptionalText(
                buyerAddress,
                MaxBuyerAddressLength,
                nameof(buyerAddress));

        var normalizedDiscount =
            NormalizeDiscount(
                discountAmount,
                Subtotal);

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (SaleDate == saleDate &&
            BuyerName == normalizedBuyerName &&
            BuyerPhone == normalizedBuyerPhone &&
            BuyerAddress == normalizedBuyerAddress &&
            PaymentTerm == paymentTerm &&
            DueDate == dueDate &&
            DiscountAmount == normalizedDiscount &&
            Notes == normalizedNotes)
        {
            return;
        }

        SaleDate = saleDate;
        BuyerName = normalizedBuyerName;
        BuyerPhone = normalizedBuyerPhone;
        BuyerAddress = normalizedBuyerAddress;
        PaymentTerm = paymentTerm;
        DueDate = dueDate;
        DiscountAmount = normalizedDiscount;
        TotalAmount = CalculateTotalAmount(
            Subtotal,
            normalizedDiscount);
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public SaleLine AddLine(
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
        EnsureDraft(
            "Lines can only be added to a draft sale.");

        if (_lines.Any(line =>
                line.HarvestBatchId == harvestBatchId))
        {
            throw new InvalidOperationException(
                "A sale can only contain one line for " +
                "each harvest batch.");
        }

        var line = SaleLine.Create(
            OrganizationId,
            Id,
            harvestBatchId,
            harvestBatchCodeSnapshot,
            cropCycleIdSnapshot,
            cropCycleCodeSnapshot,
            commodityIdSnapshot,
            commodityCodeSnapshot,
            commodityNameSnapshot,
            qualityGradeSnapshot,
            quantity,
            quantityUnit,
            unitPrice,
            lineDiscount,
            notes);

        var proposedSubtotal =
            Math.Round(
                Subtotal + line.LineTotal,
                2,
                MidpointRounding.AwayFromZero);

        EnsureDiscountFitsSubtotal(
            DiscountAmount,
            proposedSubtotal);

        _lines.Add(line);

        ApplyTotals(proposedSubtotal);
        UpdatedAt = DateTime.UtcNow;

        return line;
    }

    public void UpdateLine(
        Guid saleLineId,
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscount,
        string? notes)
    {
        EnsureDraft(
            "Lines can only be updated on a draft sale.");

        var line = FindLine(saleLineId);

        var amounts = SaleLine.CalculateAmounts(
            quantity,
            unitPrice,
            lineDiscount);

        var proposedSubtotal =
            Math.Round(
                Subtotal -
                line.LineTotal +
                amounts.LineTotal,
                2,
                MidpointRounding.AwayFromZero);

        EnsureDiscountFitsSubtotal(
            DiscountAmount,
            proposedSubtotal);

        var changed = line.Update(
            quantity,
            unitPrice,
            lineDiscount,
            notes);

        if (!changed)
        {
            return;
        }

        ApplyTotals(proposedSubtotal);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveLine(Guid saleLineId)
    {
        EnsureDraft(
            "Lines can only be removed from a draft sale.");

        var line = FindLine(saleLineId);

        var proposedSubtotal =
            Math.Round(
                Subtotal - line.LineTotal,
                2,
                MidpointRounding.AwayFromZero);

        EnsureDiscountFitsSubtotal(
            DiscountAmount,
            proposedSubtotal);

        _lines.Remove(line);

        ApplyTotals(proposedSubtotal);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        EnsureDraft(
            "Only a draft sale can be confirmed.");

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException(
                "A sale must have at least one line " +
                "before confirmation.");
        }

        EnsureDiscountFitsSubtotal(
            DiscountAmount,
            Subtotal);

        Status = SaleStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        if (Status is not SaleStatus.Draft and
            not SaleStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only a draft or confirmed sale " +
                "can be cancelled.");
        }

        CancellationReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        Status = SaleStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private SaleLine FindLine(Guid saleLineId)
    {
        if (saleLineId == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale line identifier cannot be empty.",
                nameof(saleLineId));
        }

        return _lines.SingleOrDefault(line =>
                line.Id == saleLineId)
            ?? throw new InvalidOperationException(
                $"Sale line '{saleLineId}' was not found.");
    }

    private void ApplyTotals(decimal subtotal)
    {
        Subtotal =
            Math.Round(
                subtotal,
                2,
                MidpointRounding.AwayFromZero);

        TotalAmount = CalculateTotalAmount(
            Subtotal,
            DiscountAmount);
    }

    private void EnsureDraft(string message)
    {
        if (Status != SaleStatus.Draft)
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

    private static void ValidatePaymentTerms(
        DateOnly saleDate,
        SalePaymentTerm paymentTerm,
        DateOnly? dueDate)
    {
        if (!Enum.IsDefined(paymentTerm))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentTerm),
                paymentTerm,
                "Sale payment term is not supported.");
        }

        if (paymentTerm == SalePaymentTerm.Credit &&
            !dueDate.HasValue)
        {
            throw new ArgumentException(
                "A credit sale must have a due date.",
                nameof(dueDate));
        }

        if (dueDate.HasValue &&
            dueDate.Value < saleDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dueDate),
                "Due date cannot be before sale date.");
        }
    }

    private static decimal NormalizeDiscount(
        decimal discountAmount,
        decimal subtotal)
    {
        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount),
                "Sale discount cannot be negative.");
        }

        var normalizedDiscount =
            Math.Round(
                discountAmount,
                2,
                MidpointRounding.AwayFromZero);

        EnsureDiscountFitsSubtotal(
            normalizedDiscount,
            subtotal);

        return normalizedDiscount;
    }

    private static void EnsureDiscountFitsSubtotal(
        decimal discountAmount,
        decimal subtotal)
    {
        if (discountAmount > subtotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount),
                "Sale discount cannot exceed subtotal.");
        }
    }

    private static decimal CalculateTotalAmount(
        decimal subtotal,
        decimal discountAmount)
    {
        var total =
            Math.Round(
                subtotal - discountAmount,
                2,
                MidpointRounding.AwayFromZero);

        if (total < 0)
        {
            throw new InvalidOperationException(
                "Sale total amount cannot be negative.");
        }

        return total;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Sale code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                "Sale code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!SaleCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Sale code may only contain letters, " +
                "numbers, periods, hyphens, and " +
                "underscores, and must start with " +
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
        "^[A-Z0-9][A-Z0-9._-]{0,39}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SaleCodePattern();
}
