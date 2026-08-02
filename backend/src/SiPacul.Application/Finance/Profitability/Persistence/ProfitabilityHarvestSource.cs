using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ProfitabilityHarvestSource(
    HarvestBatchStatus Status,
    HarvestQuantityUnit QuantityUnit,
    decimal NetQuantity);
