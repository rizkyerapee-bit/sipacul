using SiPacul.Shared.Results;

namespace SiPacul.Application.Harvests;

public static class HarvestBatchErrors
{
    public const string ValidationCode =
        "HarvestBatches.Validation";

    public const string OrganizationNotFoundCode =
        "HarvestBatches.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "HarvestBatches.CropCycleNotFound";

    public const string CropCycleNotInProgressCode =
        "HarvestBatches.CropCycleNotInProgress";

    public const string NotFoundCode =
        "HarvestBatches.NotFound";

    public const string CodeAlreadyExistsCode =
        "HarvestBatches.CodeAlreadyExists";

    public const string InvalidHarvestDateCode =
        "HarvestBatches.InvalidHarvestDate";

    public const string QuantityUnitConflictCode =
        "HarvestBatches.QuantityUnitConflict";

    public const string InvalidStatusTransitionCode =
        "HarvestBatches.InvalidStatusTransition";

    public const string CropCycleHasDraftHarvestsCode =
        "HarvestBatches.CropCycleHasDraftHarvests";

    public const string CropCycleHasNonCancelledHarvestsCode =
        "HarvestBatches.CropCycleHasNonCancelledHarvests";

    public const string ActiveConfirmedSaleExistsCode =
        "HarvestBatches.ActiveConfirmedSaleExists";

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

    public static Error CropCycleNotInProgress(
        Guid cropCycleId)
    {
        return Error.Conflict(
            CropCycleNotInProgressCode,
            $"Crop cycle '{cropCycleId}' is not in progress. " +
            "Harvest batches can only be created, updated, " +
            "or confirmed while cultivation is in progress.");
    }

    public static Error NotFound(
        Guid cropCycleId,
        Guid harvestBatchId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Harvest batch '{harvestBatchId}' was not found " +
            $"in crop cycle '{cropCycleId}'.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Harvest batch code '{code}' already exists " +
            "in this crop cycle.");
    }

    public static Error InvalidHarvestDate(
        DateOnly harvestDate,
        DateOnly actualStartDate)
    {
        return Error.Conflict(
            InvalidHarvestDateCode,
            $"Harvest date '{harvestDate:yyyy-MM-dd}' cannot " +
            $"be before crop cycle start date " +
            $"'{actualStartDate:yyyy-MM-dd}'.");
    }

    public static Error QuantityUnitConflict()
    {
        return Error.Conflict(
            QuantityUnitConflictCode,
            "All active harvest batches in one crop cycle " +
            "must use the same quantity unit. Cancel the " +
            "conflicting batch or use the cycle's existing unit.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error CropCycleHasDraftHarvests(
        Guid cropCycleId)
    {
        return Error.Conflict(
            CropCycleHasDraftHarvestsCode,
            $"Crop cycle '{cropCycleId}' still has draft " +
            "harvest batches. Confirm or cancel them before " +
            "completing the crop cycle.");
    }

    public static Error CropCycleHasNonCancelledHarvests(
        Guid cropCycleId)
    {
        return Error.Conflict(
            CropCycleHasNonCancelledHarvestsCode,
            $"Crop cycle '{cropCycleId}' still has active " +
            "harvest history. Cancel all harvest batches " +
            "before cancelling the crop cycle.");
    }

    public static Error ActiveConfirmedSaleExists(
        Guid harvestBatchId)
    {
        return Error.Conflict(
            ActiveConfirmedSaleExistsCode,
            $"Harvest batch '{harvestBatchId}' is referenced " +
            "by an active confirmed sale. Cancel the sale " +
            "before cancelling this harvest batch.");
    }
}
