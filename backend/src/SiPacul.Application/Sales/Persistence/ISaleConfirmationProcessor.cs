namespace SiPacul.Application.Sales.Persistence;

public interface ISaleConfirmationProcessor
{
    Task<SaleConfirmationResult> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);
}
