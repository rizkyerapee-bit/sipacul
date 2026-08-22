using SiPacul.Application.Evaluations.SeasonHistories.Contracts;
using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Application.Evaluations.SeasonHistories.Mappings;

public static class SeasonHistoryMappings
{
    public static SeasonEvaluationResponse ToResponse(
        this SeasonEvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new SeasonEvaluationResponse(
            report.OrganizationId,
            report.CropCycleId,
            report.CropCycleCode,
            report.CropCycleName,
            report.LandId,
            report.LandCode,
            report.LandName,
            report.LandPlotId,
            report.LandPlotCode,
            report.LandPlotName,
            report.CommodityId,
            report.CommodityCode,
            report.CommodityName,
            report.CropCycleStatus,
            report.PlannedStartDate,
            report.ExpectedHarvestDate,
            report.ActualStartDate,
            report.ActualHarvestDate,
            report.StartVarianceDays,
            report.HarvestVarianceDays,
            report.TotalActivityCount,
            report.CompletedActivityCount,
            report.CancelledActivityCount,
            report.PendingActivityCount,
            report.IssueActivityCount,
            report.ActivityCompletionPercentage,
            report.SopLinkedActivityCount,
            report.SopCompliantActivityCount,
            report.SopDeviatedActivityCount,
            report.SopNotEvaluatedActivityCount,
            report.SopCompliancePercentage,
            report.ConfirmedHarvestBatchCount,
            report.RecognizedRevenue,
            report.CollectedRevenue,
            report.OutstandingReceivable,
            report.TotalCultivationCost,
            report.NetProfit,
            report.ProfitMarginPercentage,
            report.ProfitabilityOutcome,
            report.CapitalFundingGap,
            report.IsReadyForReview,
            report.RequiresAttention,
            report.CriticalAttentionCount,
            report.WarningAttentionCount,
            report.InformationAttentionCount,
            report.Attentions
                .Select(attention =>
                    new SeasonEvaluationAttentionResponse(
                        attention.Code,
                        attention.Severity,
                        attention.Value))
                .ToArray(),
            report.GeneratedAt);
    }
}
