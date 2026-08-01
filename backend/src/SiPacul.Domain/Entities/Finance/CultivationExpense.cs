using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Finance;

public sealed partial class CultivationExpense :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxCodeLength = 40;

    public const int MaxDescriptionLength = 250;

    public const int MaxPayeeNameLength = 150;

    public const int MaxReferenceNumberLength = 100;

    public const int MaxEvidenceUrlLength = 1000;

    public const int MaxCancellationReasonLength = 500;

    public const int MaxNotesLength = 1000;

    private CultivationExpense()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public string Code { get; private set; } =
        string.Empty;

    public DateOnly ExpenseDate { get; private set; }

    public CultivationExpenseCategory Category
    {
        get;
        private set;
    }

    public string Description { get; private set; } =
        string.Empty;

    public decimal Amount { get; private set; }

    public string? PayeeName { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public string? EvidenceUrl { get; private set; }

    public string? Notes { get; private set; }

    public CultivationExpenseStatus Status
    {
        get;
        private set;
    } = CultivationExpenseStatus.Draft;

    public DateTime? ConfirmedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public bool IsRecognizedCost =>
        Status == CultivationExpenseStatus.Confirmed;

    public static CultivationExpense Create(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        DateOnly expenseDate,
        CultivationExpenseCategory category,
        string description,
        decimal amount,
        string? payeeName,
        string? referenceNumber,
        string? evidenceUrl,
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

        ValidateDate(expenseDate);

        ValidateCategory(category);

        return new CultivationExpense
        {
            OrganizationId = organizationId,
            CropCycleId = cropCycleId,
            Code = NormalizeCode(code),
            ExpenseDate = expenseDate,
            Category = category,
            Description = NormalizeRequiredText(
                description,
                MaxDescriptionLength,
                nameof(description),
                "Expense description"),
            Amount = NormalizeAmount(amount),
            PayeeName = NormalizeOptionalText(
                payeeName,
                MaxPayeeNameLength,
                nameof(payeeName)),
            ReferenceNumber = NormalizeOptionalText(
                referenceNumber,
                MaxReferenceNumberLength,
                nameof(referenceNumber)),
            EvidenceUrl = NormalizeOptionalText(
                evidenceUrl,
                MaxEvidenceUrlLength,
                nameof(evidenceUrl)),
            Notes = NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes)),
            Status = CultivationExpenseStatus.Draft
        };
    }

    public void UpdateDraft(
        DateOnly expenseDate,
        CultivationExpenseCategory category,
        string description,
        decimal amount,
        string? payeeName,
        string? referenceNumber,
        string? evidenceUrl,
        string? notes)
    {
        EnsureDraft(
            "Only a draft cultivation expense can be updated.");

        ValidateDate(expenseDate);
        ValidateCategory(category);

        var normalizedDescription =
            NormalizeRequiredText(
                description,
                MaxDescriptionLength,
                nameof(description),
                "Expense description");

        var normalizedAmount =
            NormalizeAmount(amount);

        var normalizedPayee =
            NormalizeOptionalText(
                payeeName,
                MaxPayeeNameLength,
                nameof(payeeName));

        var normalizedReference =
            NormalizeOptionalText(
                referenceNumber,
                MaxReferenceNumberLength,
                nameof(referenceNumber));

        var normalizedEvidence =
            NormalizeOptionalText(
                evidenceUrl,
                MaxEvidenceUrlLength,
                nameof(evidenceUrl));

        var normalizedNotes =
            NormalizeOptionalText(
                notes,
                MaxNotesLength,
                nameof(notes));

        if (ExpenseDate == expenseDate &&
            Category == category &&
            Description == normalizedDescription &&
            Amount == normalizedAmount &&
            PayeeName == normalizedPayee &&
            ReferenceNumber == normalizedReference &&
            EvidenceUrl == normalizedEvidence &&
            Notes == normalizedNotes)
        {
            return;
        }

        ExpenseDate = expenseDate;
        Category = category;
        Description = normalizedDescription;
        Amount = normalizedAmount;
        PayeeName = normalizedPayee;
        ReferenceNumber = normalizedReference;
        EvidenceUrl = normalizedEvidence;
        Notes = normalizedNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        EnsureDraft(
            "Only a draft cultivation expense can be confirmed.");

        Status = CultivationExpenseStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancellationReason)
    {
        if (Status is not CultivationExpenseStatus.Draft and
            not CultivationExpenseStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only a draft or confirmed cultivation " +
                "expense can be cancelled.");
        }

        CancellationReason =
            NormalizeRequiredText(
                cancellationReason,
                MaxCancellationReasonLength,
                nameof(cancellationReason),
                "Cancellation reason");

        Status = CultivationExpenseStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureDraft(string message)
    {
        if (Status != CultivationExpenseStatus.Draft)
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

    private static void ValidateDate(DateOnly expenseDate)
    {
        if (expenseDate == default)
        {
            throw new ArgumentException(
                "Expense date must be provided.",
                nameof(expenseDate));
        }
    }

    private static void ValidateCategory(
        CultivationExpenseCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Cultivation expense category is not supported.");
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
                "Cultivation expense amount must be " +
                "greater than zero.");
        }

        return normalizedAmount;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Cultivation expense code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                "Cultivation expense code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!CultivationExpenseCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Cultivation expense code may only contain " +
                "letters, numbers, periods, hyphens, and " +
                "underscores, and must start with a letter " +
                "or number.",
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
        CultivationExpenseCodePattern();
}
