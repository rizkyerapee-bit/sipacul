using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Finance;

public sealed partial class SalePayment :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxReferenceNumberLength = 100;

    public const int MaxReceivedFromLength = 150;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private SalePayment()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid SaleId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public DateOnly PaymentDate { get; private set; }

    public decimal Amount { get; private set; }

    public SalePaymentMethod PaymentMethod
    {
        get;
        private set;
    }

    public string? ReferenceNumber { get; private set; }

    public string? ReceivedFrom { get; private set; }

    public string? Notes { get; private set; }

    public SalePaymentStatus Status
    {
        get;
        private set;
    } = SalePaymentStatus.Draft;

    public DateTime? ConfirmedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public bool IsCollectedRevenue =>
        Status == SalePaymentStatus.Confirmed;

    public static SalePayment Create(
        Guid organizationId,
        Guid saleId,
        string code,
        DateOnly paymentDate,
        decimal amount,
        SalePaymentMethod paymentMethod,
        string? referenceNumber,
        string? receivedFrom,
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

        ValidatePaymentDate(paymentDate);
        ValidatePaymentMethod(paymentMethod);

        return new SalePayment
        {
            OrganizationId = organizationId,
            SaleId = saleId,
            Code = NormalizeCode(code),
            PaymentDate = paymentDate,
            Amount = NormalizeAmount(amount),
            PaymentMethod = paymentMethod,
            ReferenceNumber =
                NormalizeOptionalText(
                    referenceNumber,
                    MaxReferenceNumberLength,
                    nameof(referenceNumber)),
            ReceivedFrom =
                NormalizeOptionalText(
                    receivedFrom,
                    MaxReceivedFromLength,
                    nameof(receivedFrom)),
            Notes =
                NormalizeOptionalText(
                    notes,
                    MaxNotesLength,
                    nameof(notes)),
            Status = SalePaymentStatus.Draft
        };
    }

    public void UpdateDraft(
        DateOnly paymentDate,
        decimal amount,
        SalePaymentMethod paymentMethod,
        string? referenceNumber,
        string? receivedFrom,
        string? notes)
    {
        EnsureDraft(
            "Only a draft sale payment can be updated.");

        ValidatePaymentDate(paymentDate);
        ValidatePaymentMethod(paymentMethod);

        var normalizedAmount =
            NormalizeAmount(amount);

        var normalizedReferenceNumber =
            NormalizeOptionalText(
                referenceNumber,
                MaxReferenceNumberLength,
                nameof(referenceNumber));

        var normalizedReceivedFrom =
            NormalizeOptionalText(
                receivedFrom,
                MaxReceivedFromLength,
                nameof(receivedFrom));

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (PaymentDate == paymentDate &&
            Amount == normalizedAmount &&
            PaymentMethod == paymentMethod &&
            ReferenceNumber ==
                normalizedReferenceNumber &&
            ReceivedFrom == normalizedReceivedFrom &&
            Notes == normalizedNotes)
        {
            return;
        }

        PaymentDate = paymentDate;
        Amount = normalizedAmount;
        PaymentMethod = paymentMethod;
        ReferenceNumber = normalizedReferenceNumber;
        ReceivedFrom = normalizedReceivedFrom;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        EnsureDraft(
            "Only a draft sale payment can be confirmed.");

        Status = SalePaymentStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        if (Status is not SalePaymentStatus.Draft and
            not SalePaymentStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only a draft or confirmed sale payment " +
                "can be cancelled.");
        }

        CancellationReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        Status = SalePaymentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureDraft(string message)
    {
        if (Status != SalePaymentStatus.Draft)
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

    private static void ValidatePaymentDate(
        DateOnly paymentDate)
    {
        if (paymentDate == default)
        {
            throw new ArgumentException(
                "Payment date must be provided.",
                nameof(paymentDate));
        }
    }

    private static void ValidatePaymentMethod(
        SalePaymentMethod paymentMethod)
    {
        if (!Enum.IsDefined(paymentMethod))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentMethod),
                paymentMethod,
                "Sale payment method is not supported.");
        }
    }

    private static decimal NormalizeAmount(decimal amount)
    {
        var normalizedAmount =
            Math.Round(
                amount,
                2,
                MidpointRounding.AwayFromZero);

        if (normalizedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Sale payment amount must be " +
                "greater than zero.");
        }

        return normalizedAmount;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Sale payment code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                $"Sale payment code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!CodePattern().IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Sale payment code may only contain letters, " +
                "numbers, periods, hyphens, and underscores, " +
                "and must start with a letter or number.",
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
    private static partial Regex CodePattern();
}
