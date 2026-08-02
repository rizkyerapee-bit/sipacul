using SiPacul.Domain.Entities.Finance.ProfitSharing;

namespace SiPacul.Application.Finance.ProfitSharing.Persistence;

public enum ProfitSharingFinalizationFailure
{
    None = 0,

    SettlementNotFound = 1,

    InvalidStatus = 2,

    ActiveSettlementExists = 3,

    CropCycleNotTerminal = 4,

    ActiveActivityExists = 5,

    DraftHarvestExists = 6,

    UnsoldHarvestExists = 7,

    DraftSaleExists = 8,

    OutstandingReceivableExists = 9,

    DraftExpenseExists = 10,

    DraftContributionExists = 11,

    DraftPaymentExists = 12,

    CapitalDoesNotMatchCost = 13,

    ZeroCostUnsupported = 14,

    SourceDataChanged = 15,

    ConcurrencyConflict = 16
}

public sealed record ProfitSharingFinalizationResult(
    ProfitSharingSettlement? Settlement,
    ProfitSharingFinalizationFailure Failure,
    decimal OutstandingReceivable = 0,
    decimal TotalCapital = 0,
    decimal TotalCost = 0,
    string? Message = null)
{
    public bool IsSuccess =>
        Failure == ProfitSharingFinalizationFailure.None &&
        Settlement is not null;

    public static ProfitSharingFinalizationResult Succeeded(
        ProfitSharingSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        return new ProfitSharingFinalizationResult(
            settlement,
            ProfitSharingFinalizationFailure.None);
    }

    public static ProfitSharingFinalizationResult Failed(
        ProfitSharingFinalizationFailure failure,
        decimal outstandingReceivable = 0,
        decimal totalCapital = 0,
        decimal totalCost = 0,
        string? message = null)
    {
        if (failure ==
            ProfitSharingFinalizationFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed finalization must specify a failure.");
        }

        return new ProfitSharingFinalizationResult(
            null,
            failure,
            outstandingReceivable,
            totalCapital,
            totalCost,
            message);
    }
}
