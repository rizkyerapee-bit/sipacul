using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements;

public static class ProfitSharingWaterfallSettlementErrors
{
    private const string Prefix =
        "ProfitSharingWaterfallSettlements.";

    public static Error Validation(string message) =>
        Error.Validation(Prefix + "Validation", message);

    public static Error OrganizationNotFound(Guid organizationId) =>
        Error.NotFound(
            Prefix + "OrganizationNotFound",
            $"Organization '{organizationId}' was not found.");

    public static Error CropCycleNotFound(Guid cropCycleId) =>
        Error.NotFound(
            Prefix + "CropCycleNotFound",
            $"Crop cycle '{cropCycleId}' was not found in this " +
            "organization.");

    public static Error AssignmentNotFound(Guid cropCycleId) =>
        Error.NotFound(
            Prefix + "AssignmentNotFound",
            $"Crop cycle '{cropCycleId}' does not have an assigned " +
            "profit-sharing scheme.");

    public static Error NotFound(Guid settlementId) =>
        Error.NotFound(
            Prefix + "NotFound",
            $"Waterfall settlement '{settlementId}' was not found " +
            "in this crop cycle.");

    public static Error CodeAlreadyExists(string code) =>
        Error.Conflict(
            Prefix + "CodeAlreadyExists",
            $"Waterfall settlement code '{code}' already exists " +
            "in this crop cycle.");

    public static Error ActiveSettlementExists(Guid cropCycleId) =>
        Error.Conflict(
            Prefix + "ActiveSettlementExists",
            $"Crop cycle '{cropCycleId}' already has an active " +
            "final settlement from SIPACUL-PS-1 or SIPACUL-PS-2.");

    public static Error CropCycleNotTerminal() =>
        Error.Conflict(
            Prefix + "CropCycleNotTerminal",
            "The crop cycle must be completed or cancelled before " +
            "finalization.");

    public static Error ActiveActivityExists() =>
        Error.Conflict(
            Prefix + "ActiveActivityExists",
            "One or more cultivation activities are still planned " +
            "or in progress.");

    public static Error DraftHarvestExists() =>
        Error.Conflict(
            Prefix + "DraftHarvestExists",
            "One or more harvest batches are still in draft.");

    public static Error UnsoldHarvestExists() =>
        Error.Conflict(
            Prefix + "UnsoldHarvestExists",
            "Confirmed harvested quantity has not been fully sold.");

    public static Error DraftSaleExists() =>
        Error.Conflict(
            Prefix + "DraftSaleExists",
            "One or more related sales are still in draft.");

    public static Error OutstandingReceivableExists(decimal amount) =>
        Error.Conflict(
            Prefix + "OutstandingReceivableExists",
            $"Outstanding receivable must be zero. Current amount " +
            $"is {amount:0.00}.");

    public static Error DraftExpenseExists() =>
        Error.Conflict(
            Prefix + "DraftExpenseExists",
            "One or more cultivation expenses are still in draft.");

    public static Error DraftContributionExists() =>
        Error.Conflict(
            Prefix + "DraftContributionExists",
            "One or more capital contributions are still in draft.");

    public static Error DraftPaymentExists() =>
        Error.Conflict(
            Prefix + "DraftPaymentExists",
            "One or more related sale payments are still in draft.");

    public static Error CapitalDoesNotMatchCost(
        decimal totalCapital,
        decimal totalCost) =>
        Error.Conflict(
            Prefix + "CapitalDoesNotMatchCost",
            $"Confirmed capital '{totalCapital:0.00}' must equal " +
            $"cultivation cost '{totalCost:0.00}'.");

    public static Error ZeroCostUnsupported() =>
        Error.Conflict(
            Prefix + "ZeroCostUnsupported",
            "A zero-cost crop cycle cannot be finalized.");

    public static Error CapitalIdentityConflict(string code) =>
        Error.Conflict(
            Prefix + "CapitalIdentityConflict",
            $"Confirmed capital code '{code}' uses inconsistent " +
            "contributor identity data.");

    public static Error CapitalNotInScheme(string code) =>
        Error.Conflict(
            Prefix + "CapitalNotInScheme",
            $"Confirmed capital code '{code}' is not a participant " +
            "in the assigned scheme.");

    public static Error CapitalRoleMismatch(string code) =>
        Error.Conflict(
            Prefix + "CapitalRoleMismatch",
            $"Confirmed capital role for '{code}' does not match " +
            "the assigned participant role.");

    public static Error SourceDataChanged() =>
        Error.Conflict(
            Prefix + "SourceDataChanged",
            "Settlement sources changed while finalization was in " +
            "progress. Reload the preview and retry.");

    public static Error CalculationUnavailable(string message) =>
        Error.Conflict(
            Prefix + "CalculationUnavailable",
            "The waterfall settlement cannot be calculated. " +
            message);

    public static Error InvalidStatus(string message) =>
        Error.Conflict(
            Prefix + "InvalidStatusTransition",
            message);

    public static Error ConcurrencyConflict() =>
        Error.Conflict(
            Prefix + "ConcurrencyConflict",
            "Another settlement operation changed this crop cycle. " +
            "Reload the data and retry.");
}
