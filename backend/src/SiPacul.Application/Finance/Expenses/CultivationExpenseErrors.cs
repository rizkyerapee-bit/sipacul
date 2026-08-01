using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.Expenses;

public static class CultivationExpenseErrors
{
    public const string ValidationCode =
        "CultivationExpenses.Validation";

    public const string OrganizationNotFoundCode =
        "CultivationExpenses.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "CultivationExpenses.CropCycleNotFound";

    public const string NotFoundCode =
        "CultivationExpenses.NotFound";

    public const string CodeAlreadyExistsCode =
        "CultivationExpenses.CodeAlreadyExists";

    public const string InvalidStatusTransitionCode =
        "CultivationExpenses.InvalidStatusTransition";

    public const string DateOutOfRangeCode =
        "CultivationExpenses.DateOutOfRange";

    public const string FinalizedSettlementExistsCode =
        "CultivationExpenses.FinalizedSettlementExists";

    public static Error Validation(string message)
    {
        return Error.Validation(
            ValidationCode,
            message);
    }

    public static Error OrganizationNotFound(
        Guid organizationId)
    {
        return Error.NotFound(
            OrganizationNotFoundCode,
            $"Organization '{organizationId}' was not found.");
    }

    public static Error CropCycleNotFound(
        Guid cropCycleId)
    {
        return Error.NotFound(
            CropCycleNotFoundCode,
            $"Crop cycle '{cropCycleId}' was not found " +
            "in this organization.");
    }

    public static Error NotFound(Guid expenseId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Cultivation expense '{expenseId}' was not found " +
            "in this crop cycle.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Cultivation expense code '{code}' already " +
            "exists in this crop cycle.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error DateOutOfRange(
        DateOnly expenseDate,
        DateOnly earliestDate,
        DateOnly latestDate)
    {
        return Error.Validation(
            DateOutOfRangeCode,
            $"Expense date '{expenseDate:yyyy-MM-dd}' must be " +
            $"between '{earliestDate:yyyy-MM-dd}' and " +
            $"'{latestDate:yyyy-MM-dd}'.");
    }

    public static Error FinalizedSettlementExists(
        Guid cropCycleId)
    {
        return Error.Conflict(
            FinalizedSettlementExistsCode,
            $"Crop cycle '{cropCycleId}' already has an active " +
            "finalized settlement. Its expenses are locked.");
    }
}
