using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.SalePayments.Persistence;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class SalePaymentRepository :
    ISalePaymentRepository
{
    private readonly SiPaculDbContext _dbContext;

    public SalePaymentRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SalePayment>>
        GetAllAsync(
            Guid organizationId,
            Guid saleId,
            SalePaymentStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            DateOnly? paymentDateFrom = null,
            DateOnly? paymentDateTo = null,
            string? receivedFrom = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<SalePayment> query =
            _dbContext.SalePayments
                .AsNoTracking()
                .Where(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    !payment.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(payment =>
                payment.Status == status.Value);
        }

        if (paymentMethod.HasValue)
        {
            query = query.Where(payment =>
                payment.PaymentMethod ==
                    paymentMethod.Value);
        }

        if (paymentDateFrom.HasValue)
        {
            query = query.Where(payment =>
                payment.PaymentDate >=
                    paymentDateFrom.Value);
        }

        if (paymentDateTo.HasValue)
        {
            query = query.Where(payment =>
                payment.PaymentDate <=
                    paymentDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(receivedFrom))
        {
            var pattern =
                $"%{receivedFrom.Trim()}%";

            query = query.Where(payment =>
                payment.ReceivedFrom != null &&
                EF.Functions.ILike(
                    payment.ReceivedFrom,
                    pattern));
        }

        return await query
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<SalePayment?> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SalePayments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    payment.Id == paymentId &&
                    !payment.IsDeleted,
                cancellationToken);
    }

    public Task<SalePayment?>
        GetByIdForUpdateAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.SalePayments
            .SingleOrDefaultAsync(
                payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    payment.Id == paymentId &&
                    !payment.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SalePayments
            .AsNoTracking()
            .AnyAsync(
                payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.Code == code &&
                    !payment.IsDeleted,
                cancellationToken);
    }

    public async Task<decimal>
        GetConfirmedPaidAmountAsync(
            Guid organizationId,
            Guid saleId,
            Guid? excludedPaymentId = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<SalePayment> query =
            _dbContext.SalePayments
                .AsNoTracking()
                .Where(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    payment.Status ==
                        SalePaymentStatus.Confirmed &&
                    !payment.IsDeleted);

        if (excludedPaymentId.HasValue)
        {
            query = query.Where(payment =>
                payment.Id !=
                    excludedPaymentId.Value);
        }

        var total = await query
            .Select(payment =>
                (decimal?)payment.Amount)
            .SumAsync(cancellationToken);

        return Math.Round(
            total ?? 0,
            2,
            MidpointRounding.AwayFromZero);
    }

    public Task<bool> HasConfirmedPaymentsAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SalePayments
            .AsNoTracking()
            .AnyAsync(
                payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    payment.Status ==
                        SalePaymentStatus.Confirmed &&
                    !payment.IsDeleted,
                cancellationToken);
    }

    public void Add(SalePayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        _dbContext.SalePayments.Add(payment);
    }
}
