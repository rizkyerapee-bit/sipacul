using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Finance;

public sealed partial class CapitalContribution :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxContributorCodeLength = 40;

    public const int MaxContributorNameLength = 150;

    public const int MaxReferenceNumberLength = 100;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private CapitalContribution()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public DateOnly ContributionDate { get; private set; }

    public string ContributorCode { get; private set; } =
        string.Empty;

    public string ContributorName { get; private set; } =
        string.Empty;

    public CapitalContributorRole ContributorRole
    {
        get;
        private set;
    }

    public decimal Amount { get; private set; }

    public CapitalContributionPaymentMethod PaymentMethod
    {
        get;
        private set;
    }

    public string? ReferenceNumber { get; private set; }

    public string? Notes { get; private set; }

    public CapitalContributionStatus Status
    {
        get;
        private set;
    } = CapitalContributionStatus.Draft;

    public DateTime? ConfirmedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public bool IsConfirmedCapital =>
        Status == CapitalContributionStatus.Confirmed;

    public bool IsInvestorCapital =>
        ContributorRole == CapitalContributorRole.Investor;

    public bool IsPartnerCapital =>
        ContributorRole == CapitalContributorRole.Partner;

    public static CapitalContribution Create(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        DateOnly contributionDate,
        string contributorCode,
        string contributorName,
        CapitalContributorRole contributorRole,
        decimal amount,
        CapitalContributionPaymentMethod paymentMethod,
        string? referenceNumber,
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

        ValidateDate(contributionDate);
        ValidateContributorRole(contributorRole);
        ValidatePaymentMethod(paymentMethod);

        return new CapitalContribution
        {
            OrganizationId = organizationId,
            CropCycleId = cropCycleId,
            Code = NormalizeTransactionCode(code),
            ContributionDate = contributionDate,
            ContributorCode =
                NormalizeContributorCode(
                    contributorCode),
            ContributorName =
                NormalizeRequiredText(
                    contributorName,
                    MaxContributorNameLength,
                    nameof(contributorName),
                    "Contributor name"),
            ContributorRole = contributorRole,
            Amount = NormalizeAmount(amount),
            PaymentMethod = paymentMethod,
            ReferenceNumber =
                NormalizeOptionalText(
                    referenceNumber,
                    MaxReferenceNumberLength,
                    nameof(referenceNumber)),
            Notes =
                NormalizeOptionalText(
                    notes,
                    MaxNotesLength,
                    nameof(notes)),
            Status = CapitalContributionStatus.Draft
        };
    }

    public void UpdateDraft(
        DateOnly contributionDate,
        string contributorCode,
        string contributorName,
        CapitalContributorRole contributorRole,
        decimal amount,
        CapitalContributionPaymentMethod paymentMethod,
        string? referenceNumber,
        string? notes)
    {
        EnsureDraft(
            "Only a draft capital contribution can be updated.");

        ValidateDate(contributionDate);
        ValidateContributorRole(contributorRole);
        ValidatePaymentMethod(paymentMethod);

        var normalizedContributorCode =
            NormalizeContributorCode(
                contributorCode);

        var normalizedContributorName =
            NormalizeRequiredText(
                contributorName,
                MaxContributorNameLength,
                nameof(contributorName),
                "Contributor name");

        var normalizedAmount =
            NormalizeAmount(amount);

        var normalizedReference =
            NormalizeOptionalText(
                referenceNumber,
                MaxReferenceNumberLength,
                nameof(referenceNumber));

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (ContributionDate == contributionDate &&
            ContributorCode == normalizedContributorCode &&
            ContributorName == normalizedContributorName &&
            ContributorRole == contributorRole &&
            Amount == normalizedAmount &&
            PaymentMethod == paymentMethod &&
            ReferenceNumber == normalizedReference &&
            Notes == normalizedNotes)
        {
            return;
        }

        ContributionDate = contributionDate;
        ContributorCode = normalizedContributorCode;
        ContributorName = normalizedContributorName;
        ContributorRole = contributorRole;
        Amount = normalizedAmount;
        PaymentMethod = paymentMethod;
        ReferenceNumber = normalizedReference;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        EnsureDraft(
            "Only a draft capital contribution can be confirmed.");

        Status = CapitalContributionStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        if (Status is not CapitalContributionStatus.Draft and
            not CapitalContributionStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only a draft or confirmed capital " +
                "contribution can be cancelled.");
        }

        CancellationReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        Status = CapitalContributionStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureDraft(string message)
    {
        if (Status != CapitalContributionStatus.Draft)
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
        DateOnly contributionDate)
    {
        if (contributionDate == default)
        {
            throw new ArgumentException(
                "Contribution date must be provided.",
                nameof(contributionDate));
        }
    }

    private static void ValidateContributorRole(
        CapitalContributorRole contributorRole)
    {
        if (!Enum.IsDefined(contributorRole))
        {
            throw new ArgumentOutOfRangeException(
                nameof(contributorRole),
                contributorRole,
                "Capital contributor role is not supported.");
        }
    }

    private static void ValidatePaymentMethod(
        CapitalContributionPaymentMethod paymentMethod)
    {
        if (!Enum.IsDefined(paymentMethod))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentMethod),
                paymentMethod,
                "Capital contribution payment method " +
                "is not supported.");
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
                "Capital contribution amount must be " +
                "greater than zero.");
        }

        return normalizedAmount;
    }

    private static string NormalizeTransactionCode(
        string code)
    {
        return NormalizeCode(
            code,
            MaxCodeLength,
            nameof(code),
            "Capital contribution code");
    }

    private static string NormalizeContributorCode(
        string contributorCode)
    {
        return NormalizeCode(
            contributorCode,
            MaxContributorCodeLength,
            nameof(contributorCode),
            "Contributor code");
    }

    private static string NormalizeCode(
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

        var normalizedValue =
            value.Trim().ToUpperInvariant();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        if (!CodePattern().IsMatch(normalizedValue))
        {
            throw new ArgumentException(
                $"{displayName} may only contain letters, " +
                "numbers, periods, hyphens, and underscores, " +
                "and must start with a letter or number.",
                parameterName);
        }

        return normalizedValue;
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
