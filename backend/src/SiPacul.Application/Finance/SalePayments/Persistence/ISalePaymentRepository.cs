using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.SalePayments.Persistence;

public interface ISalePaymentRepository
{
    Task<IReadOnlyList<SalePayment>> GetAllAsync(
        Guid organizationId,
        Guid saleId,
        SalePaymentStatus? status = null,
        SalePaymentMethod? paymentMethod = null,
        DateOnly? paymentDateFrom = null,
        DateOnly? paymentDateTo = null,
        string? receivedFrom = null,
        CancellationToken cancellationToken = default);

    Task<SalePayment?> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<SalePayment?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<decimal> GetConfirmedPaidAmountAsync(
        Guid organizationId,
        Guid saleId,
        Guid? excludedPaymentId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasConfirmedPaymentsAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    void Add(SalePayment payment);
}
