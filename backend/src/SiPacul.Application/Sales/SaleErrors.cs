using SiPacul.Domain.Entities.Harvests;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Sales;

public static class SaleErrors
{
    public const string ValidationCode =
        "Sales.Validation";

    public const string OrganizationNotFoundCode =
        "Sales.OrganizationNotFound";

    public const string NotFoundCode =
        "Sales.NotFound";

    public const string CodeAlreadyExistsCode =
        "Sales.CodeAlreadyExists";

    public const string InvalidStatusTransitionCode =
        "Sales.InvalidStatusTransition";

    public const string LineNotFoundCode =
        "Sales.LineNotFound";

    public const string HarvestBatchNotFoundCode =
        "Sales.HarvestBatchNotFound";

    public const string HarvestBatchNotConfirmedCode =
        "Sales.HarvestBatchNotConfirmed";

    public const string QuantityUnitMismatchCode =
        "Sales.QuantityUnitMismatch";

    public const string InsufficientQuantityCode =
        "Sales.InsufficientQuantity";

    public const string ConfirmationConcurrencyCode =
        "Sales.ConfirmationConcurrency";

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

    public static Error NotFound(Guid saleId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Sale '{saleId}' was not found in this organization.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Sale code '{code}' already exists " +
            "in this organization.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error LineNotFound(
        Guid saleId,
        Guid saleLineId)
    {
        return Error.NotFound(
            LineNotFoundCode,
            $"Sale line '{saleLineId}' was not found " +
            $"in sale '{saleId}'.");
    }

    public static Error HarvestBatchNotFound(
        Guid harvestBatchId)
    {
        return Error.NotFound(
            HarvestBatchNotFoundCode,
            $"Harvest batch '{harvestBatchId}' was not found " +
            "in this organization.");
    }

    public static Error HarvestBatchNotConfirmed(
        Guid harvestBatchId)
    {
        return Error.Conflict(
            HarvestBatchNotConfirmedCode,
            $"Harvest batch '{harvestBatchId}' is not confirmed " +
            "and cannot be sold.");
    }

    public static Error QuantityUnitMismatch(
        Guid harvestBatchId,
        HarvestQuantityUnit expected,
        HarvestQuantityUnit actual)
    {
        return Error.Conflict(
            QuantityUnitMismatchCode,
            $"Harvest batch '{harvestBatchId}' uses unit " +
            $"'{expected}', but the sale line uses '{actual}'. " +
            "Automatic unit conversion is not supported.");
    }

    public static Error InsufficientQuantity(
        Guid harvestBatchId,
        decimal requestedQuantity,
        decimal availableQuantity)
    {
        return Error.Conflict(
            InsufficientQuantityCode,
            $"Harvest batch '{harvestBatchId}' only has " +
            $"'{availableQuantity}' available, but " +
            $"'{requestedQuantity}' was requested.");
    }

    public static Error ConfirmationConcurrency()
    {
        return Error.Conflict(
            ConfirmationConcurrencyCode,
            "The sale could not be confirmed because harvest " +
            "availability changed concurrently. Reload the sale " +
            "and try again.");
    }

    public const string ConfirmedPaymentsExistCode =
            "Sales.ConfirmedPaymentsExist";

    public static Error ConfirmedPaymentsExist(
        Guid saleId)
    {
        return Error.Conflict(
            ConfirmedPaymentsExistCode,
            $"Sale '{saleId}' has confirmed payments. " +
            "Cancel those payments before cancelling the sale.");
    }
}
