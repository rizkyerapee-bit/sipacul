using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;

public enum ProfitSharingWaterfallSettlementFailure
{
    None = 0,
    CropCycleNotFound = 1,
    AssignmentNotFound = 2,
    SettlementNotFound = 3,
    CodeAlreadyExists = 4,
    ActiveSettlementExists = 5,
    CropCycleNotTerminal = 6,
    ActiveActivityExists = 7,
    DraftHarvestExists = 8,
    UnsoldHarvestExists = 9,
    DraftSaleExists = 10,
    OutstandingReceivableExists = 11,
    DraftExpenseExists = 12,
    DraftContributionExists = 13,
    DraftPaymentExists = 14,
    CapitalDoesNotMatchCost = 15,
    ZeroCostUnsupported = 16,
    CapitalIdentityConflict = 17,
    CapitalNotInScheme = 18,
    CapitalRoleMismatch = 19,
    SourceDataChanged = 20,
    CalculationUnavailable = 21,
    InvalidStatus = 22,
    ConcurrencyConflict = 23,
    Validation = 24
}

public sealed record ProfitSharingWaterfallSettlementOperationResult(
    ProfitSharingWaterfallSettlement? Settlement,
    ProfitSharingWaterfallSettlementFailure Failure,
    string? Code = null,
    string? ContributorCode = null,
    decimal OutstandingReceivable = 0,
    decimal TotalCapital = 0,
    decimal TotalCost = 0,
    string? Message = null)
{
    public bool IsSuccess =>
        Failure == ProfitSharingWaterfallSettlementFailure.None &&
        Settlement is not null;

    public static ProfitSharingWaterfallSettlementOperationResult
        Succeeded(ProfitSharingWaterfallSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        return new ProfitSharingWaterfallSettlementOperationResult(
            settlement,
            ProfitSharingWaterfallSettlementFailure.None);
    }

    public static ProfitSharingWaterfallSettlementOperationResult Failed(
        ProfitSharingWaterfallSettlementFailure failure,
        string? code = null,
        string? contributorCode = null,
        decimal outstandingReceivable = 0,
        decimal totalCapital = 0,
        decimal totalCost = 0,
        string? message = null)
    {
        if (failure == ProfitSharingWaterfallSettlementFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed operation must specify a failure.");
        }

        return new ProfitSharingWaterfallSettlementOperationResult(
            null,
            failure,
            code,
            contributorCode,
            outstandingReceivable,
            totalCapital,
            totalCost,
            message);
    }
}
