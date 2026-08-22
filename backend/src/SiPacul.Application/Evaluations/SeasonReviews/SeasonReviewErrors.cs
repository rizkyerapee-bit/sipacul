using SiPacul.Shared.Results;

namespace SiPacul.Application.Evaluations.SeasonReviews;

public static class SeasonReviewErrors
{
    public const string ValidationCode = "SeasonReviews.Validation";
    public const string CropCycleNotFoundCode = "SeasonReviews.CropCycleNotFound";
    public const string CropCycleNotTerminalCode = "SeasonReviews.CropCycleNotTerminal";
    public const string AlreadyExistsCode = "SeasonReviews.AlreadyExists";
    public const string NotFoundCode = "SeasonReviews.NotFound";
    public const string InvalidStatusTransitionCode = "SeasonReviews.InvalidStatusTransition";

    public static Error Validation(string message) => Error.Validation(ValidationCode, message);

    public static Error CropCycleNotFound(Guid organizationId, Guid cropCycleId) =>
        Error.NotFound(CropCycleNotFoundCode,
            $"Crop cycle '{cropCycleId}' was not found in organization '{organizationId}'.");

    public static Error CropCycleNotTerminal(Guid cropCycleId) =>
        Error.Conflict(CropCycleNotTerminalCode,
            $"Crop cycle '{cropCycleId}' must be completed or cancelled before it can be reviewed.");

    public static Error AlreadyExists(Guid cropCycleId) =>
        Error.Conflict(AlreadyExistsCode,
            $"An active season review already exists for crop cycle '{cropCycleId}'.");

    public static Error NotFound(Guid organizationId, Guid reviewId) =>
        Error.NotFound(NotFoundCode,
            $"Season review '{reviewId}' was not found in organization '{organizationId}'.");

    public static Error InvalidStatusTransition(string message) =>
        Error.Conflict(InvalidStatusTransitionCode, message);
}
