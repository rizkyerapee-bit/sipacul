using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing;

public static class ProfitSharingSettlementErrors
{
    public const string ValidationCode =
        "ProfitSharingSettlements.Validation";

    public const string OrganizationNotFoundCode =
        "ProfitSharingSettlements.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "ProfitSharingSettlements.CropCycleNotFound";

    public const string NotFoundCode =
        "ProfitSharingSettlements.NotFound";

    public const string CodeAlreadyExistsCode =
        "ProfitSharingSettlements.CodeAlreadyExists";

    public const string ActiveSettlementExistsCode =
        "ProfitSharingSettlements.ActiveSettlementExists";

    public const string CropCycleNotTerminalCode =
        "ProfitSharingSettlements.CropCycleNotTerminal";

    public const string ActiveActivityExistsCode =
        "ProfitSharingSettlements.ActiveActivityExists";

    public const string DraftHarvestExistsCode =
        "ProfitSharingSettlements.DraftHarvestExists";

    public const string UnsoldHarvestExistsCode =
        "ProfitSharingSettlements.UnsoldHarvestExists";

    public const string DraftSaleExistsCode =
        "ProfitSharingSettlements.DraftSaleExists";

    public const string OutstandingReceivableExistsCode =
        "ProfitSharingSettlements.OutstandingReceivableExists";

    public const string DraftExpenseExistsCode =
        "ProfitSharingSettlements.DraftExpenseExists";

    public const string DraftContributionExistsCode =
        "ProfitSharingSettlements.DraftContributionExists";

    public const string DraftPaymentExistsCode =
        "ProfitSharingSettlements.DraftPaymentExists";

    public const string CapitalDoesNotMatchCostCode =
        "ProfitSharingSettlements.CapitalDoesNotMatchCost";

    public const string ZeroCostUnsupportedCode =
        "ProfitSharingSettlements.ZeroCostUnsupported";

    public const string SourceDataChangedCode =
        "ProfitSharingSettlements.SourceDataChanged";

    public const string InvalidStatusTransitionCode =
        "ProfitSharingSettlements.InvalidStatusTransition";

    public const string ConcurrencyConflictCode =
        "ProfitSharingSettlements.ConcurrencyConflict";

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

    public static Error NotFound(Guid settlementId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Profit sharing settlement '{settlementId}' " +
            "was not found in this crop cycle.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Profit sharing settlement code '{code}' already " +
            "exists in this crop cycle.");
    }

    public static Error ActiveSettlementExists(
        Guid cropCycleId)
    {
        return Error.Conflict(
            ActiveSettlementExistsCode,
            $"Crop cycle '{cropCycleId}' already has an active " +
            "finalized profit sharing settlement.");
    }

    public static Error CropCycleNotTerminal()
    {
        return Error.Conflict(
            CropCycleNotTerminalCode,
            "The crop cycle must be completed or cancelled " +
            "before settlement finalization.");
    }

    public static Error ActiveActivityExists()
    {
        return Error.Conflict(
            ActiveActivityExistsCode,
            "One or more cultivation activities are still " +
            "planned or in progress.");
    }

    public static Error DraftHarvestExists()
    {
        return Error.Conflict(
            DraftHarvestExistsCode,
            "One or more harvest batches are still in draft.");
    }

    public static Error UnsoldHarvestExists()
    {
        return Error.Conflict(
            UnsoldHarvestExistsCode,
            "Confirmed harvested quantity has not been fully sold.");
    }

    public static Error DraftSaleExists()
    {
        return Error.Conflict(
            DraftSaleExistsCode,
            "One or more related sales are still in draft.");
    }

    public static Error OutstandingReceivableExists(
        decimal outstandingReceivable)
    {
        return Error.Conflict(
            OutstandingReceivableExistsCode,
            $"Outstanding receivable must be zero. Current " +
            $"amount is {outstandingReceivable:0.00}.");
    }

    public static Error DraftExpenseExists()
    {
        return Error.Conflict(
            DraftExpenseExistsCode,
            "One or more cultivation expenses are still in draft.");
    }

    public static Error DraftContributionExists()
    {
        return Error.Conflict(
            DraftContributionExistsCode,
            "One or more capital contributions are still in draft.");
    }

    public static Error DraftPaymentExists()
    {
        return Error.Conflict(
            DraftPaymentExistsCode,
            "One or more related sale payments are still in draft.");
    }

    public static Error CapitalDoesNotMatchCost(
        decimal totalCapital,
        decimal totalCost)
    {
        return Error.Conflict(
            CapitalDoesNotMatchCostCode,
            $"Confirmed capital '{totalCapital:0.00}' must equal " +
            $"total cultivation cost '{totalCost:0.00}'.");
    }

    public static Error ZeroCostUnsupported()
    {
        return Error.Conflict(
            ZeroCostUnsupportedCode,
            "A zero-cost crop cycle cannot be settled.");
    }

    public static Error SourceDataChanged()
    {
        return Error.Conflict(
            SourceDataChangedCode,
            "Settlement source data changed after the draft " +
            "snapshot was created.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error ConcurrencyConflict()
    {
        return Error.Conflict(
            ConcurrencyConflictCode,
            "The settlement was changed by another operation. " +
            "Reload the data and retry.");
    }
}
