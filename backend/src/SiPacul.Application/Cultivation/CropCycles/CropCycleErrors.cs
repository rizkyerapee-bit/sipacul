using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.CropCycles;

public static class CropCycleErrors
{
    public const string ValidationCode =
        "CropCycles.Validation";

    public const string OrganizationNotFoundCode =
        "CropCycles.OrganizationNotFound";

    public const string NotFoundCode =
        "CropCycles.NotFound";

    public const string CodeAlreadyExistsCode =
        "CropCycles.CodeAlreadyExists";

    public const string CommodityNotFoundCode =
        "CropCycles.CommodityNotFound";

    public const string CommodityInactiveCode =
        "CropCycles.CommodityInactive";

    public const string LandNotFoundCode =
        "CropCycles.LandNotFound";

    public const string LandInactiveCode =
        "CropCycles.LandInactive";

    public const string PlotNotFoundCode =
        "CropCycles.PlotNotFound";

    public const string PlotInactiveCode =
        "CropCycles.PlotInactive";

    public const string SopNotFoundCode =
        "CropCycles.SopNotFound";

    public const string SopInactiveCode =
        "CropCycles.SopInactive";

    public const string SopCommodityMismatchCode =
        "CropCycles.SopCommodityMismatch";

    public const string AreaCapacityExceededCode =
        "CropCycles.AreaCapacityExceeded";

    public const string ScheduleConflictCode =
        "CropCycles.ScheduleConflict";

    public const string ActiveCycleAlreadyExistsCode =
        "CropCycles.ActiveCycleAlreadyExists";

    public const string InvalidStatusTransitionCode =
        "CropCycles.InvalidStatusTransition";

    public const string ActiveReferenceExistsCode =
        "CropCycles.ActiveReferenceExists";

    public const string HistoricalReferenceExistsCode =
        "CropCycles.HistoricalReferenceExists";

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

    public static Error NotFound(
        Guid organizationId,
        Guid cropCycleId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Crop cycle '{cropCycleId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Crop cycle code '{code}' already exists " +
            "in this organization.");
    }

    public static Error CommodityNotFound(Guid commodityId)
    {
        return Error.NotFound(
            CommodityNotFoundCode,
            $"Commodity '{commodityId}' was not found " +
            "in this organization.");
    }

    public static Error CommodityInactive(Guid commodityId)
    {
        return Error.Conflict(
            CommodityInactiveCode,
            $"Commodity '{commodityId}' is inactive.");
    }

    public static Error LandNotFound(Guid landId)
    {
        return Error.NotFound(
            LandNotFoundCode,
            $"Land '{landId}' was not found " +
            "in this organization.");
    }

    public static Error LandInactive(Guid landId)
    {
        return Error.Conflict(
            LandInactiveCode,
            $"Land '{landId}' is inactive.");
    }

    public static Error PlotNotFound(
        Guid landId,
        Guid landPlotId)
    {
        return Error.NotFound(
            PlotNotFoundCode,
            $"Land plot '{landPlotId}' was not found " +
            $"in land '{landId}'.");
    }

    public static Error PlotInactive(Guid landPlotId)
    {
        return Error.Conflict(
            PlotInactiveCode,
            $"Land plot '{landPlotId}' is inactive.");
    }

    public static Error SopNotFound(Guid cultivationSopId)
    {
        return Error.NotFound(
            SopNotFoundCode,
            $"Cultivation SOP '{cultivationSopId}' " +
            "was not found in this organization.");
    }

    public static Error SopInactive(Guid cultivationSopId)
    {
        return Error.Conflict(
            SopInactiveCode,
            $"Cultivation SOP '{cultivationSopId}' is inactive.");
    }

    public static Error SopCommodityMismatch(
        Guid cultivationSopId,
        Guid commodityId)
    {
        return Error.Conflict(
            SopCommodityMismatchCode,
            $"Cultivation SOP '{cultivationSopId}' does not " +
            $"belong to commodity '{commodityId}'.");
    }

    public static Error AreaCapacityExceeded(
        decimal plantedAreaInSquareMeters,
        decimal plotAreaInSquareMeters)
    {
        return Error.Conflict(
            AreaCapacityExceededCode,
            $"Planted area '{plantedAreaInSquareMeters}' square " +
            $"meters exceeds plot capacity " +
            $"'{plotAreaInSquareMeters}' square meters.");
    }

    public static Error ScheduleConflict(
        Guid landPlotId)
    {
        return Error.Conflict(
            ScheduleConflictCode,
            $"The planned cultivation period conflicts with " +
            $"another crop cycle on plot '{landPlotId}'.");
    }

    public static Error ActiveCycleAlreadyExists(
        Guid landPlotId)
    {
        return Error.Conflict(
            ActiveCycleAlreadyExistsCode,
            $"Another in-progress crop cycle already exists " +
            $"on plot '{landPlotId}'.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error ActiveReferenceExists(
        string resourceName,
        Guid resourceId)
    {
        return Error.Conflict(
            ActiveReferenceExistsCode,
            $"{resourceName} '{resourceId}' cannot be " +
            "deactivated because it has a planned or " +
            "in-progress crop cycle.");
    }

    public static Error HistoricalReferenceExists(
        Guid landPlotId)
    {
        return Error.Conflict(
            HistoricalReferenceExistsCode,
            $"Land plot '{landPlotId}' cannot be removed " +
            "because it is referenced by crop-cycle history.");
    }
}
