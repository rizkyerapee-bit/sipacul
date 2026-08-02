namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ProfitabilitySaleLineSource(
    Guid SaleLineId,
    Guid CropCycleId,
    decimal LineTotal,
    decimal Quantity);
