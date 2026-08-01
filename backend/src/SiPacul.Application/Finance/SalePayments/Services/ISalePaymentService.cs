using SiPacul.Application.Finance.SalePayments.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.SalePayments.Services;

public interface ISalePaymentService
{
    Task<Result<SalePaymentResponse>> CreateAsync(
        Guid organizationId,
        Guid saleId,
        CreateSalePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SalePaymentResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid saleId,
            SalePaymentFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<SalePaymentResponse>> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<Result<SalePaymentResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        UpdateSalePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SalePaymentResponse>> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<Result<SalePaymentResponse>> CancelAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancelSalePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SaleReceivableResponse>>
        GetReceivableAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default);
}
