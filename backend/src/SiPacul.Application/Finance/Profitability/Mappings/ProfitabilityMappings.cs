using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Finance.Profitability.Mappings;

public static class ProfitabilityMappings
{
    public static CropCycleProfitabilityResponse ToResponse(
        this CropCycleProfitabilityReport report,
        HarvestQuantityUnit? harvestQuantityUnit)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new CropCycleProfitabilityResponse(
            report.OrganizationId,
            report.CropCycleId,
            report.CropCycleCode,
            report.CropCycleName,
            report.CommodityIdSnapshot,
            report.CommodityCodeSnapshot,
            report.CommodityNameSnapshot,
            report.RecognizedRevenue,
            report.CollectedRevenue,
            report.OutstandingReceivable,
            report.ActivityResourceCost,
            report.ManualExpenseCost,
            report.TotalCultivationCost,
            report.NetProfit,
            report.ProfitMarginPercentage,
            report.Outcome,
            report.ConfirmedInvestorCapital,
            report.ConfirmedPartnerCapital,
            report.TotalConfirmedCapital,
            report.CapitalFundingGap,
            report.CapitalFundingExcess,
            report.AvailableHarvestQuantity,
            harvestQuantityUnit,
            report.GeneratedAt);
    }
}
