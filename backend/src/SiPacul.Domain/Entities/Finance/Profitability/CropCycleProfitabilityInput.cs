namespace SiPacul.Domain.Entities.Finance.Profitability;

public sealed record CropCycleProfitabilityInput(
    Guid OrganizationId,
    Guid CropCycleId,
    string CropCycleCode,
    string CropCycleName,
    Guid CommodityIdSnapshot,
    string CommodityCodeSnapshot,
    string CommodityNameSnapshot,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal ActivityResourceCost,
    decimal ManualExpenseCost,
    decimal ConfirmedInvestorCapital,
    decimal ConfirmedPartnerCapital,
    decimal AvailableHarvestQuantity,
    DateTime GeneratedAt);
