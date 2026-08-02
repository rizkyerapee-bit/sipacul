using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ProfitabilitySourceSnapshot(
    Guid OrganizationId,
    Guid CropCycleId,
    string CropCycleCode,
    string CropCycleName,
    Guid CommodityId,
    string CommodityCode,
    string CommodityName,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal ActivityResourceCost,
    decimal ManualExpenseCost,
    decimal ConfirmedInvestorCapital,
    decimal ConfirmedPartnerCapital,
    decimal AvailableHarvestQuantity,
    HarvestQuantityUnit? HarvestQuantityUnit)
{
    public CropCycleProfitabilityInput ToInput(
        DateTime generatedAt)
    {
        return new CropCycleProfitabilityInput(
            OrganizationId,
            CropCycleId,
            CropCycleCode,
            CropCycleName,
            CommodityId,
            CommodityCode,
            CommodityName,
            RecognizedRevenue,
            CollectedRevenue,
            ActivityResourceCost,
            ManualExpenseCost,
            ConfirmedInvestorCapital,
            ConfirmedPartnerCapital,
            AvailableHarvestQuantity,
            generatedAt);
    }
}
