using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class SaleConfirmationProcessor :
    ISaleConfirmationProcessor
{
    private readonly SiPaculDbContext _dbContext;

    private readonly ISaleRepository _saleRepository;

    public SaleConfirmationProcessor(
        SiPaculDbContext dbContext,
        ISaleRepository saleRepository)
    {
        _dbContext = dbContext;
        _saleRepository = saleRepository;
    }

    public async Task<SaleConfirmationResult> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var executionStrategy =
            _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(
                async () =>
                {
                    _dbContext.ChangeTracker.Clear();

                    await using var transaction =
                        await _dbContext.Database
                            .BeginTransactionAsync(
                                IsolationLevel.Serializable,
                                cancellationToken);

                    var result =
                        await ConfirmWithinTransactionAsync(
                            organizationId,
                            saleId,
                            cancellationToken);

                    if (!result.IsSuccess)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return result;
                    }

                    await transaction.CommitAsync(
                        cancellationToken);

                    return result;
                });
        }
        catch (Exception exception)
            when (IsSerializationFailure(exception))
        {
            return SaleConfirmationResult.Failed(
                SaleConfirmationFailure
                    .ConcurrencyConflict);
        }
    }

    private async Task<SaleConfirmationResult>
        ConfirmWithinTransactionAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken)
    {
        var sale = await _saleRepository
            .GetByIdForUpdateAsync(
                organizationId,
                saleId,
                cancellationToken);

        if (sale is null)
        {
            return SaleConfirmationResult.Failed(
                SaleConfirmationFailure.SaleNotFound);
        }

        if (sale.Status != SaleStatus.Draft)
        {
            return SaleConfirmationResult.Failed(
                SaleConfirmationFailure.InvalidStatus,
                message:
                    "Only a draft sale can be confirmed.");
        }

        if (sale.Lines.Count == 0)
        {
            return SaleConfirmationResult.Failed(
                SaleConfirmationFailure.EmptySale,
                message:
                    "A sale must have at least one line " +
                    "before confirmation.");
        }

        var harvestBatchIds = sale.Lines
            .Select(line => line.HarvestBatchId)
            .Distinct()
            .ToArray();

        var soldQuantities =
            await _saleRepository
                .GetConfirmedSoldQuantitiesAsync(
                    organizationId,
                    harvestBatchIds,
                    cancellationToken);

        foreach (var line in sale.Lines)
        {
            var reference = await _saleRepository
                .GetHarvestReferenceAsync(
                    organizationId,
                    line.HarvestBatchId,
                    cancellationToken);

            if (reference is null)
            {
                return SaleConfirmationResult.Failed(
                    SaleConfirmationFailure
                        .HarvestBatchNotFound,
                    line.HarvestBatchId);
            }

            if (reference.Status !=
                HarvestBatchStatus.Confirmed)
            {
                return SaleConfirmationResult.Failed(
                    SaleConfirmationFailure
                        .HarvestBatchNotConfirmed,
                    line.HarvestBatchId);
            }

            if (reference.QuantityUnit !=
                line.QuantityUnit)
            {
                return SaleConfirmationResult.Failed(
                    SaleConfirmationFailure
                        .QuantityUnitMismatch,
                    line.HarvestBatchId,
                    message:
                        "Sale line quantity unit does not " +
                        "match the harvest batch unit.");
            }

            var soldQuantity =
                soldQuantities.TryGetValue(
                    line.HarvestBatchId,
                    out var currentSoldQuantity)
                    ? currentSoldQuantity
                    : 0;

            var availableQuantity = Math.Round(
                Math.Max(
                    reference.NetQuantity -
                        soldQuantity,
                    0),
                4,
                MidpointRounding.AwayFromZero);

            if (line.Quantity > availableQuantity)
            {
                return SaleConfirmationResult.Failed(
                    SaleConfirmationFailure
                        .InsufficientQuantity,
                    line.HarvestBatchId,
                    line.Quantity,
                    availableQuantity);
            }
        }

        try
        {
            sale.Confirm();
        }
        catch (InvalidOperationException exception)
        {
            return SaleConfirmationResult.Failed(
                SaleConfirmationFailure.InvalidStatus,
                message: exception.Message);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return SaleConfirmationResult.Succeeded(sale);
    }

    private static bool IsSerializationFailure(
        Exception exception)
    {
        if (exception is PostgresException postgresException &&
            postgresException.SqlState ==
                PostgresErrorCodes.SerializationFailure)
        {
            return true;
        }

        if (exception is DbUpdateException updateException &&
            updateException.InnerException is not null &&
            IsSerializationFailure(
                updateException.InnerException))
        {
            return true;
        }

        return exception.InnerException is not null &&
            IsSerializationFailure(
                exception.InnerException);
    }
}
