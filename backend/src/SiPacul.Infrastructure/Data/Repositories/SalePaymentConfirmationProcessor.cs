using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiPacul.Application.Finance.SalePayments.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class SalePaymentConfirmationProcessor :
    ISalePaymentConfirmationProcessor
{
    private readonly SiPaculDbContext _dbContext;

    public SalePaymentConfirmationProcessor(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SalePaymentConfirmationResult>
        ConfirmAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
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
                            paymentId,
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
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .ConcurrencyConflict);
        }
    }

    private async Task<SalePaymentConfirmationResult>
        ConfirmWithinTransactionAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            CancellationToken cancellationToken)
    {
        var payment =
            await _dbContext.SalePayments
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OrganizationId ==
                            organizationId &&
                        candidate.SaleId == saleId &&
                        candidate.Id == paymentId &&
                        !candidate.IsDeleted,
                    cancellationToken);

        if (payment is null)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .PaymentNotFound);
        }

        var sale = await _dbContext.Sales
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId ==
                        organizationId &&
                    candidate.Id == saleId &&
                    !candidate.IsDeleted,
                cancellationToken);

        if (sale is null)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .SaleNotFound);
        }

        if (sale.Status != SaleStatus.Confirmed)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .SaleNotConfirmed);
        }

        if (payment.Status != SalePaymentStatus.Draft)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .InvalidStatus,
                message:
                    "Only a draft sale payment " +
                    "can be confirmed.");
        }

        if (payment.PaymentDate < sale.SaleDate)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .PaymentDateBeforeSaleDate,
                sale.SaleDate);
        }

        var currentConfirmedPaidAmount =
            await _dbContext.SalePayments
                .AsNoTracking()
                .Where(candidate =>
                    candidate.OrganizationId ==
                        organizationId &&
                    candidate.SaleId == saleId &&
                    candidate.Id != paymentId &&
                    candidate.Status ==
                        SalePaymentStatus.Confirmed &&
                    !candidate.IsDeleted)
                .Select(candidate =>
                    (decimal?)candidate.Amount)
                .SumAsync(cancellationToken) ??
            0;

        var proposedConfirmedPaidAmount =
            Math.Round(
                currentConfirmedPaidAmount +
                    payment.Amount,
                2,
                MidpointRounding.AwayFromZero);

        if (proposedConfirmedPaidAmount >
            sale.TotalAmount)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .Overpayment,
                confirmedPaidAmount:
                    proposedConfirmedPaidAmount,
                saleTotalAmount:
                    sale.TotalAmount);
        }

        try
        {
            payment.Confirm();
        }
        catch (InvalidOperationException exception)
        {
            return SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .InvalidStatus,
                message: exception.Message);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return SalePaymentConfirmationResult.Succeeded(
            payment,
            proposedConfirmedPaidAmount,
            sale.TotalAmount);
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
