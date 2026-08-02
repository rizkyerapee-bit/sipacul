using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Finance.Profitability.Contracts;

public sealed record CropCycleProfitabilityResponse(
    Guid OrganizationId,
    Guid CropCycleId,
    string CropCycleCode,
    string CropCycleName,
    Guid CommodityIdSnapshot,
    string CommodityCodeSnapshot,
    string CommodityNameSnapshot,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal OutstandingReceivable,
    decimal ActivityResourceCost,
    decimal ManualExpenseCost,
    decimal TotalCultivationCost,
    decimal NetProfit,
    decimal? ProfitMarginPercentage,
    ProfitabilityOutcome Outcome,
    decimal ConfirmedInvestorCapital,
    decimal ConfirmedPartnerCapital,
    decimal TotalConfirmedCapital,
    decimal CapitalFundingGap,
    decimal CapitalFundingExcess,
    decimal AvailableHarvestQuantity,
    HarvestQuantityUnit? HarvestQuantityUnit,
    DateTime GeneratedAt);
