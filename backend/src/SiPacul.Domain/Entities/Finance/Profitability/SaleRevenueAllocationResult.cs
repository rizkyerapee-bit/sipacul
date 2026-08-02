namespace SiPacul.Domain.Entities.Finance.Profitability;

public sealed record SaleRevenueAllocationResult(
    decimal Subtotal,
    decimal SaleDiscountAmount,
    decimal SaleTotalAmount,
    decimal ConfirmedPaymentAmount,
    IReadOnlyList<SaleLineRevenueAllocation> Lines)
{
    public decimal RecognizedRevenue =>
        Math.Round(
            Lines.Sum(line =>
                line.NetRecognizedRevenue),
            2,
            MidpointRounding.AwayFromZero);

    public decimal CollectedRevenue =>
        Math.Round(
            Lines.Sum(line =>
                line.AllocatedCollectedRevenue),
            2,
            MidpointRounding.AwayFromZero);

    public decimal OutstandingReceivable =>
        Math.Round(
            RecognizedRevenue - CollectedRevenue,
            2,
            MidpointRounding.AwayFromZero);
}
