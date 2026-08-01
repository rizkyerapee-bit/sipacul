using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Persistence;

public interface ISaleRepository
{
    Task<IReadOnlyList<Sale>> GetAllAsync(
        Guid organizationId,
        SaleStatus? status = null,
        DateOnly? saleDateFrom = null,
        DateOnly? saleDateTo = null,
        SalePaymentTerm? paymentTerm = null,
        string? buyerName = null,
        CancellationToken cancellationToken = default);

    Task<Sale?> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<Sale?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<SaleHarvestReference?> GetHarvestReferenceAsync(
        Guid organizationId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>>
        GetConfirmedSoldQuantitiesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> harvestBatchIds,
            CancellationToken cancellationToken = default);

    Task<decimal> GetConfirmedSoldQuantityAsync(
        Guid organizationId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveConfirmedSaleForHarvestAsync(
        Guid organizationId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default);

    void Add(Sale sale);
}
