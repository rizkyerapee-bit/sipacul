using SiPacul.Shared.Results;

namespace SiPacul.Application.Lands;

public static class LandErrors
{
    public const string ValidationCode =
        "Lands.Validation";

    public const string OrganizationNotFoundCode =
        "Lands.OrganizationNotFound";

    public const string NotFoundCode =
        "Lands.NotFound";

    public const string CodeAlreadyExistsCode =
        "Lands.CodeAlreadyExists";

    public const string PlotNotFoundCode =
        "Lands.PlotNotFound";

    public const string PlotCodeAlreadyExistsCode =
        "Lands.PlotCodeAlreadyExists";

    public const string AreaCapacityExceededCode =
        "Lands.AreaCapacityExceeded";

    public const string HistoricalReferenceExistsCode =
        "Lands.HistoricalReferenceExists";

    public static Error Validation(
        string message)
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

    public static Error NotFound(
        Guid organizationId,
        Guid landId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Land '{landId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error CodeAlreadyExists(
        string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Land code '{code}' already exists " +
            "in this organization.");
    }

    public static Error PlotNotFound(
        Guid landId,
        Guid plotId)
    {
        return Error.NotFound(
            PlotNotFoundCode,
            $"Land plot '{plotId}' was not found " +
            $"in land '{landId}'.");
    }

    public static Error PlotCodeAlreadyExists(
        Guid landId,
        string code)
    {
        return Error.Conflict(
            PlotCodeAlreadyExistsCode,
            $"Land plot code '{code}' already exists " +
            $"in land '{landId}'.");
    }

    public static Error AreaCapacityExceeded(
        string message)
    {
        return Error.Conflict(
            AreaCapacityExceededCode,
            message);
    }

    public static Error HistoricalReferenceExists(
        Guid landId)
    {
        return Error.Conflict(
            HistoricalReferenceExistsCode,
            $"Land '{landId}' cannot be removed because " +
            "it is referenced by crop-cycle history.");
    }
}
