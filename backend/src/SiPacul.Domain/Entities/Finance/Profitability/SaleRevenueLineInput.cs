namespace SiPacul.Domain.Entities.Finance.Profitability;

public sealed record SaleRevenueLineInput(
    Guid SaleLineId,
    Guid CropCycleId,
    decimal LineTotal);
