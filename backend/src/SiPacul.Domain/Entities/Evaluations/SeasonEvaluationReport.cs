using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Evaluations;

public sealed record SeasonEvaluationReport
{
    public Guid OrganizationId { get; internal init; }

    public Guid CropCycleId { get; internal init; }

    public string CropCycleCode { get; internal init; } =
        string.Empty;

    public string CropCycleName { get; internal init; } =
        string.Empty;

    public Guid LandId { get; internal init; }

    public string LandCode { get; internal init; } =
        string.Empty;

    public string LandName { get; internal init; } =
        string.Empty;

    public Guid LandPlotId { get; internal init; }

    public string LandPlotCode { get; internal init; } =
        string.Empty;

    public string LandPlotName { get; internal init; } =
        string.Empty;

    public Guid CommodityId { get; internal init; }

    public string CommodityCode { get; internal init; } =
        string.Empty;

    public string CommodityName { get; internal init; } =
        string.Empty;

    public CropCycleStatus CropCycleStatus
    {
        get;
        internal init;
    }

    public DateOnly PlannedStartDate { get; internal init; }

    public DateOnly ExpectedHarvestDate { get; internal init; }

    public DateOnly? ActualStartDate { get; internal init; }

    public DateOnly? ActualHarvestDate { get; internal init; }

    public int? StartVarianceDays { get; internal init; }

    public int? HarvestVarianceDays { get; internal init; }

    public int TotalActivityCount { get; internal init; }

    public int CompletedActivityCount { get; internal init; }

    public int CancelledActivityCount { get; internal init; }

    public int PendingActivityCount { get; internal init; }

    public int IssueActivityCount { get; internal init; }

    public decimal? ActivityCompletionPercentage
    {
        get;
        internal init;
    }

    public int SopLinkedActivityCount { get; internal init; }

    public int SopCompliantActivityCount { get; internal init; }

    public int SopDeviatedActivityCount { get; internal init; }

    public int SopNotEvaluatedActivityCount
    {
        get;
        internal init;
    }

    public decimal? SopCompliancePercentage
    {
        get;
        internal init;
    }

    public int ConfirmedHarvestBatchCount
    {
        get;
        internal init;
    }

    public decimal RecognizedRevenue { get; internal init; }

    public decimal CollectedRevenue { get; internal init; }

    public decimal OutstandingReceivable { get; internal init; }

    public decimal TotalCultivationCost { get; internal init; }

    public decimal NetProfit { get; internal init; }

    public decimal? ProfitMarginPercentage
    {
        get;
        internal init;
    }

    public ProfitabilityOutcome ProfitabilityOutcome
    {
        get;
        internal init;
    }

    public decimal CapitalFundingGap { get; internal init; }

    public bool IsReadyForReview { get; internal init; }

    public bool RequiresAttention { get; internal init; }

    public int CriticalAttentionCount { get; internal init; }

    public int WarningAttentionCount { get; internal init; }

    public int InformationAttentionCount { get; internal init; }

    public IReadOnlyList<SeasonEvaluationAttention> Attentions
    {
        get;
        internal init;
    } = Array.Empty<SeasonEvaluationAttention>();

    public DateTime GeneratedAt { get; internal init; }
}
