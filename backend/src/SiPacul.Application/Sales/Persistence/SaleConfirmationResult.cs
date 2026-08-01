using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Persistence;

public enum SaleConfirmationFailure
{
    None = 0,
    SaleNotFound = 1,
    InvalidStatus = 2,
    EmptySale = 3,
    HarvestBatchNotFound = 4,
    HarvestBatchNotConfirmed = 5,
    QuantityUnitMismatch = 6,
    InsufficientQuantity = 7,
    ConcurrencyConflict = 8
}

public sealed record SaleConfirmationResult(
    Sale? Sale,
    SaleConfirmationFailure Failure,
    Guid? HarvestBatchId = null,
    decimal RequestedQuantity = 0,
    decimal AvailableQuantity = 0,
    string? Message = null)
{
    public bool IsSuccess =>
        Failure == SaleConfirmationFailure.None &&
        Sale is not null;

    public static SaleConfirmationResult Succeeded(
        Sale sale)
    {
        ArgumentNullException.ThrowIfNull(sale);

        return new SaleConfirmationResult(
            sale,
            SaleConfirmationFailure.None);
    }

    public static SaleConfirmationResult Failed(
        SaleConfirmationFailure failure,
        Guid? harvestBatchId = null,
        decimal requestedQuantity = 0,
        decimal availableQuantity = 0,
        string? message = null)
    {
        if (failure == SaleConfirmationFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed confirmation must specify a failure.");
        }

        return new SaleConfirmationResult(
            null,
            failure,
            harvestBatchId,
            requestedQuantity,
            availableQuantity,
            message);
    }
}
