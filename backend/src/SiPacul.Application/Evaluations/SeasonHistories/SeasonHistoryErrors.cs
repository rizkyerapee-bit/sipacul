using SiPacul.Shared.Results;

namespace SiPacul.Application.Evaluations.SeasonHistories;

public static class SeasonHistoryErrors
{
    public const string ValidationCode =
        "SeasonHistory.Validation";

    public const string LandNotFoundCode =
        "SeasonHistory.LandNotFound";

    public const string LandPlotNotFoundCode =
        "SeasonHistory.LandPlotNotFound";

    public const string SourceDataInvalidCode =
        "SeasonHistory.SourceDataInvalid";

    public static Error Validation(string message)
    {
        return Error.Validation(
            ValidationCode,
            message);
    }

    public static Error LandNotFound(
        Guid organizationId,
        Guid landId)
    {
        return Error.NotFound(
            LandNotFoundCode,
            $"Land '{landId}' was not found in " +
            $"organization '{organizationId}'.");
    }

    public static Error LandPlotNotFound(
        Guid landId,
        Guid landPlotId)
    {
        return Error.NotFound(
            LandPlotNotFoundCode,
            $"Land plot '{landPlotId}' was not found " +
            $"in land '{landId}'.");
    }

    public static Error SourceDataInvalid(string message)
    {
        return Error.Failure(
            SourceDataInvalidCode,
            $"Season history source data is invalid: {message}");
    }
}
