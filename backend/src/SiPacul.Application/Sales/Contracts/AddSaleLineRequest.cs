using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Sales.Contracts;

public sealed record AddSaleLineRequest(
    Guid HarvestBatchId,
    decimal Quantity,
    HarvestQuantityUnit QuantityUnit,
    decimal UnitPrice,
    decimal LineDiscount,
    string? Notes);
