namespace SiPacul.Application.Finance.SalePayments.Persistence;

public interface ISalePaymentConfirmationProcessor
{
    Task<SalePaymentConfirmationResult> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancellationToken cancellationToken = default);
}
