using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Application.Finance.ProfitSharing.Calculations;

public enum ProfitSharingWaterfallSourceFailure
{
    None = 0,

    CapitalIdentityConflict = 1,

    CapitalNotInScheme = 2,

    CapitalRoleMismatch = 3,

    SourceDataChanged = 4,

    CalculationUnavailable = 5
}

public sealed record ProfitSharingWaterfallSourceCalculation(
    CropCycleProfitabilityReport? Profitability,
    ProfitSharingWaterfallCalculationResult? Calculation,
    ProfitSharingWaterfallSourceFailure Failure,
    string? ContributorCode = null,
    string? Message = null)
{
    public bool IsSuccess =>
        Failure == ProfitSharingWaterfallSourceFailure.None &&
        Profitability is not null &&
        Calculation is not null;

    public static ProfitSharingWaterfallSourceCalculation Succeeded(
        CropCycleProfitabilityReport profitability,
        ProfitSharingWaterfallCalculationResult calculation)
    {
        ArgumentNullException.ThrowIfNull(profitability);
        ArgumentNullException.ThrowIfNull(calculation);

        return new ProfitSharingWaterfallSourceCalculation(
            profitability,
            calculation,
            ProfitSharingWaterfallSourceFailure.None);
    }

    public static ProfitSharingWaterfallSourceCalculation Failed(
        ProfitSharingWaterfallSourceFailure failure,
        string? contributorCode = null,
        string? message = null)
    {
        if (failure == ProfitSharingWaterfallSourceFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed source calculation must specify a failure.");
        }

        return new ProfitSharingWaterfallSourceCalculation(
            null,
            null,
            failure,
            contributorCode,
            message);
    }
}
