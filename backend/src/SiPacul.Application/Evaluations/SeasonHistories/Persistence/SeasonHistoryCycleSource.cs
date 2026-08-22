using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Application.Evaluations.SeasonHistories.Persistence;

public sealed record SeasonHistoryCycleSource(
    Guid OrganizationId,
    Guid CropCycleId,
    string CropCycleCode,
    string CropCycleName,
    Guid LandId,
    Guid LandPlotId,
    Guid CommodityId,
    string CommodityCode,
    string CommodityName,
    CropCycleStatus CropCycleStatus,
    DateOnly PlannedStartDate,
    DateOnly ExpectedHarvestDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualHarvestDate,
    int TotalActivityCount,
    int CompletedActivityCount,
    int CancelledActivityCount,
    int PendingActivityCount,
    int IssueActivityCount,
    int SopLinkedActivityCount,
    int SopCompliantActivityCount,
    int SopDeviatedActivityCount,
    int SopNotEvaluatedActivityCount,
    int ConfirmedHarvestBatchCount,
    decimal RecognizedRevenue,
    decimal CollectedRevenue,
    decimal TotalCultivationCost,
    decimal CapitalFundingGap)
{
    public SeasonEvaluationInput ToInput(
        string landCode,
        string landName,
        string landPlotCode,
        string landPlotName,
        DateTime generatedAt)
    {
        return new SeasonEvaluationInput(
            OrganizationId,
            CropCycleId,
            CropCycleCode,
            CropCycleName,
            LandId,
            landCode,
            landName,
            LandPlotId,
            landPlotCode,
            landPlotName,
            CommodityId,
            CommodityCode,
            CommodityName,
            CropCycleStatus,
            PlannedStartDate,
            ExpectedHarvestDate,
            ActualStartDate,
            ActualHarvestDate,
            TotalActivityCount,
            CompletedActivityCount,
            CancelledActivityCount,
            PendingActivityCount,
            IssueActivityCount,
            SopLinkedActivityCount,
            SopCompliantActivityCount,
            SopDeviatedActivityCount,
            SopNotEvaluatedActivityCount,
            ConfirmedHarvestBatchCount,
            RecognizedRevenue,
            CollectedRevenue,
            TotalCultivationCost,
            CapitalFundingGap,
            generatedAt);
    }
}
