using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.Activities;

public static class CultivationActivityErrors
{
    public const string ValidationCode =
        "CultivationActivities.Validation";

    public const string OrganizationNotFoundCode =
        "CultivationActivities.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "CultivationActivities.CropCycleNotFound";

    public const string CropCycleTerminalCode =
        "CultivationActivities.CropCycleTerminal";

    public const string NotFoundCode =
        "CultivationActivities.NotFound";

    public const string CodeAlreadyExistsCode =
        "CultivationActivities.CodeAlreadyExists";

    public const string SopNotFoundCode =
        "CultivationActivities.SopNotFound";

    public const string SopInactiveCode =
        "CultivationActivities.SopInactive";

    public const string SopCommodityMismatchCode =
        "CultivationActivities.SopCommodityMismatch";

    public const string SopStepNotFoundCode =
        "CultivationActivities.SopStepNotFound";

    public const string SopStepMismatchCode =
        "CultivationActivities.SopStepMismatch";

    public const string PlannedDateOutOfRangeCode =
        "CultivationActivities.PlannedDateOutOfRange";

    public const string InvalidStatusTransitionCode =
        "CultivationActivities.InvalidStatusTransition";

    public const string ResourceNotFoundCode =
        "CultivationActivities.ResourceNotFound";

    public const string CropCycleHasInProgressActivitiesCode =
        "CultivationActivities.CropCycleHasInProgressActivities";

    public const string SopStepHistoricalReferenceExistsCode =
        "CultivationActivities.SopStepHistoricalReferenceExists";

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

    public static Error CropCycleTerminal(
        Guid cropCycleId)
    {
        return Error.Conflict(
            CropCycleTerminalCode,
            $"Crop cycle '{cropCycleId}' is terminal. " +
            "Its cultivation activities are read-only.");
    }

    public static Error NotFound(
        Guid cropCycleId,
        Guid activityId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Cultivation activity '{activityId}' was not " +
            $"found in crop cycle '{cropCycleId}'.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Cultivation activity code '{code}' already " +
            "exists in this crop cycle.");
    }

    public static Error SopNotFound(Guid sopId)
    {
        return Error.NotFound(
            SopNotFoundCode,
            $"Cultivation SOP '{sopId}' was not found " +
            "in this organization.");
    }

    public static Error SopInactive(Guid sopId)
    {
        return Error.Conflict(
            SopInactiveCode,
            $"Cultivation SOP '{sopId}' is inactive.");
    }

    public static Error SopCommodityMismatch(
        Guid sopId,
        Guid commodityId)
    {
        return Error.Conflict(
            SopCommodityMismatchCode,
            $"Cultivation SOP '{sopId}' does not belong " +
            $"to commodity '{commodityId}'.");
    }

    public static Error SopStepNotFound(Guid stepId)
    {
        return Error.NotFound(
            SopStepNotFoundCode,
            $"Cultivation SOP step '{stepId}' was not found.");
    }

    public static Error SopStepMismatch(
        Guid sopId,
        Guid stepId)
    {
        return Error.Conflict(
            SopStepMismatchCode,
            $"Cultivation SOP step '{stepId}' does not " +
            $"belong to SOP '{sopId}'.");
    }

    public static Error PlannedDateOutOfRange(
        DateOnly plannedDate,
        DateOnly expectedHarvestDate)
    {
        return Error.Conflict(
            PlannedDateOutOfRangeCode,
            $"Planned activity date '{plannedDate:yyyy-MM-dd}' " +
            "cannot be after crop-cycle expected harvest date " +
            $"'{expectedHarvestDate:yyyy-MM-dd}'.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error ResourceNotFound(Guid resourceId)
    {
        return Error.NotFound(
            ResourceNotFoundCode,
            $"Cultivation activity resource '{resourceId}' " +
            "was not found.");
    }

    public static Error CropCycleHasInProgressActivities(
        Guid cropCycleId)
    {
        return Error.Conflict(
            CropCycleHasInProgressActivitiesCode,
            $"Crop cycle '{cropCycleId}' cannot complete or " +
            "be cancelled while a cultivation activity is " +
            "in progress.");
    }

    public static Error SopStepHistoricalReferenceExists(
        Guid stepId)
    {
        return Error.Conflict(
            SopStepHistoricalReferenceExistsCode,
            $"Cultivation SOP step '{stepId}' cannot be " +
            "removed because it is referenced by cultivation " +
            "activity history.");
    }
}
