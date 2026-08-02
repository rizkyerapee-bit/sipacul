namespace SiPacul.Domain.Entities.Finance.Profitability;

public sealed record CropCycleRevenueAllocation(
    Guid CropCycleId,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal OutstandingReceivable);
