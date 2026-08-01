using SiPacul.Application.Sales.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Sales.Services;

public interface ISaleService
{
    Task<Result<SaleResponse>> CreateAsync(
        Guid organizationId,
        CreateSaleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SaleResponse>>> GetAllAsync(
        Guid organizationId,
        SaleFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid saleId,
        UpdateSaleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> AddLineAsync(
        Guid organizationId,
        Guid saleId,
        AddSaleLineRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> UpdateLineAsync(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        UpdateSaleLineRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> RemoveLineAsync(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<Result<SaleResponse>> CancelAsync(
        Guid organizationId,
        Guid saleId,
        CancelSaleRequest request,
        CancellationToken cancellationToken = default);
}
