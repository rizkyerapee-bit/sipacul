namespace SiPacul.Domain.Entities.Finance.Profitability;

public sealed record SaleLineRevenueAllocation(
    Guid SaleLineId,
    Guid CropCycleId,
    decimal LineTotal,
    decimal AllocatedSaleDiscount,
    decimal NetRecognizedRevenue,
    decimal AllocatedCollectedRevenue)
{
    public decimal OutstandingReceivable =>
        Math.Round(
            NetRecognizedRevenue -
                AllocatedCollectedRevenue,
            2,
            MidpointRounding.AwayFromZero);
}
