using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Evaluations;

public static class SeasonEvaluationCalculator
{
    public static SeasonEvaluationReport Calculate(
        SeasonEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidateIdentifier(
            input.OrganizationId,
            nameof(input.OrganizationId),
            "Organization");

        ValidateIdentifier(
            input.CropCycleId,
            nameof(input.CropCycleId),
            "Crop cycle");

        ValidateIdentifier(
            input.LandId,
            nameof(input.LandId),
            "Land");

        ValidateIdentifier(
            input.LandPlotId,
            nameof(input.LandPlotId),
            "Land plot");

        ValidateIdentifier(
            input.CommodityId,
            nameof(input.CommodityId),
            "Commodity");

        ValidateStatus(input.CropCycleStatus);
        ValidateDates(input);
        ValidateActivityCounts(input);
        ValidateSopCounts(input);

        if (input.ConfirmedHarvestBatchCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.ConfirmedHarvestBatchCount),
                "Confirmed harvest batch count cannot be negative.");
        }

        var recognizedRevenue = NormalizeMoney(
            input.RecognizedRevenue,
            nameof(input.RecognizedRevenue));

        var collectedRevenue = NormalizeMoney(
            input.CollectedRevenue,
            nameof(input.CollectedRevenue));

        if (collectedRevenue > recognizedRevenue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.CollectedRevenue),
                "Collected revenue cannot exceed recognized revenue.");
        }

        var totalCultivationCost = NormalizeMoney(
            input.TotalCultivationCost,
            nameof(input.TotalCultivationCost));

        var capitalFundingGap = NormalizeMoney(
            input.CapitalFundingGap,
            nameof(input.CapitalFundingGap));

        var outstandingReceivable = NormalizeMoney(
            recognizedRevenue - collectedRevenue,
            nameof(input.CollectedRevenue));

        var netProfit = Math.Round(
            recognizedRevenue - totalCultivationCost,
            2,
            MidpointRounding.AwayFromZero);

        decimal? profitMarginPercentage =
            recognizedRevenue == 0
                ? null
                : Math.Round(
                    netProfit / recognizedRevenue * 100,
                    4,
                    MidpointRounding.AwayFromZero);

        var profitabilityOutcome =
            DetermineProfitabilityOutcome(netProfit);

        var isReadyForReview =
            input.CropCycleStatus is
                CropCycleStatus.Completed or
                CropCycleStatus.Cancelled;

        int? startVarianceDays = input.ActualStartDate.HasValue
            ? input.ActualStartDate.Value.DayNumber -
                input.PlannedStartDate.DayNumber
            : null;

        int? harvestVarianceDays = input.ActualHarvestDate.HasValue
            ? input.ActualHarvestDate.Value.DayNumber -
                input.ExpectedHarvestDate.DayNumber
            : null;

        var activityCompletionPercentage =
            CalculatePercentage(
                input.CompletedActivityCount,
                input.TotalActivityCount);

        var sopCompliancePercentage =
            CalculatePercentage(
                input.SopCompliantActivityCount,
                input.SopLinkedActivityCount);

        var attentions = BuildAttentions(
            input,
            startVarianceDays,
            harvestVarianceDays,
            outstandingReceivable,
            netProfit,
            profitabilityOutcome,
            capitalFundingGap);

        return new SeasonEvaluationReport
        {
            OrganizationId = input.OrganizationId,
            CropCycleId = input.CropCycleId,
            CropCycleCode = NormalizeRequiredText(
                input.CropCycleCode,
                nameof(input.CropCycleCode)),
            CropCycleName = NormalizeRequiredText(
                input.CropCycleName,
                nameof(input.CropCycleName)),
            LandId = input.LandId,
            LandCode = NormalizeRequiredText(
                input.LandCode,
                nameof(input.LandCode)),
            LandName = NormalizeRequiredText(
                input.LandName,
                nameof(input.LandName)),
            LandPlotId = input.LandPlotId,
            LandPlotCode = NormalizeRequiredText(
                input.LandPlotCode,
                nameof(input.LandPlotCode)),
            LandPlotName = NormalizeRequiredText(
                input.LandPlotName,
                nameof(input.LandPlotName)),
            CommodityId = input.CommodityId,
            CommodityCode = NormalizeRequiredText(
                input.CommodityCode,
                nameof(input.CommodityCode)),
            CommodityName = NormalizeRequiredText(
                input.CommodityName,
                nameof(input.CommodityName)),
            CropCycleStatus = input.CropCycleStatus,
            PlannedStartDate = input.PlannedStartDate,
            ExpectedHarvestDate = input.ExpectedHarvestDate,
            ActualStartDate = input.ActualStartDate,
            ActualHarvestDate = input.ActualHarvestDate,
            StartVarianceDays = startVarianceDays,
            HarvestVarianceDays = harvestVarianceDays,
            TotalActivityCount = input.TotalActivityCount,
            CompletedActivityCount = input.CompletedActivityCount,
            CancelledActivityCount = input.CancelledActivityCount,
            PendingActivityCount = input.PendingActivityCount,
            IssueActivityCount = input.IssueActivityCount,
            ActivityCompletionPercentage =
                activityCompletionPercentage,
            SopLinkedActivityCount = input.SopLinkedActivityCount,
            SopCompliantActivityCount =
                input.SopCompliantActivityCount,
            SopDeviatedActivityCount =
                input.SopDeviatedActivityCount,
            SopNotEvaluatedActivityCount =
                input.SopNotEvaluatedActivityCount,
            SopCompliancePercentage = sopCompliancePercentage,
            ConfirmedHarvestBatchCount =
                input.ConfirmedHarvestBatchCount,
            RecognizedRevenue = recognizedRevenue,
            CollectedRevenue = collectedRevenue,
            OutstandingReceivable = outstandingReceivable,
            TotalCultivationCost = totalCultivationCost,
            NetProfit = netProfit,
            ProfitMarginPercentage = profitMarginPercentage,
            ProfitabilityOutcome = profitabilityOutcome,
            CapitalFundingGap = capitalFundingGap,
            IsReadyForReview = isReadyForReview,
            RequiresAttention = attentions.Any(attention =>
                attention.Severity is
                    SeasonEvaluationAttentionSeverity.Warning or
                    SeasonEvaluationAttentionSeverity.Critical),
            CriticalAttentionCount = attentions.Count(attention =>
                attention.Severity ==
                    SeasonEvaluationAttentionSeverity.Critical),
            WarningAttentionCount = attentions.Count(attention =>
                attention.Severity ==
                    SeasonEvaluationAttentionSeverity.Warning),
            InformationAttentionCount = attentions.Count(attention =>
                attention.Severity ==
                    SeasonEvaluationAttentionSeverity.Information),
            Attentions = attentions,
            GeneratedAt = NormalizeGeneratedAt(input.GeneratedAt)
        };
    }

    private static IReadOnlyList<SeasonEvaluationAttention>
        BuildAttentions(
            SeasonEvaluationInput input,
            int? startVarianceDays,
            int? harvestVarianceDays,
            decimal outstandingReceivable,
            decimal netProfit,
            ProfitabilityOutcome profitabilityOutcome,
            decimal capitalFundingGap)
    {
        var attentions =
            new List<SeasonEvaluationAttention>();

        if (input.CropCycleStatus is
            CropCycleStatus.Planned or
            CropCycleStatus.InProgress)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.CycleNotTerminal,
                SeasonEvaluationAttentionSeverity.Warning);
        }

        if (input.CropCycleStatus == CropCycleStatus.Cancelled)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.CycleCancelled,
                SeasonEvaluationAttentionSeverity.Critical);
        }

        if (startVarianceDays > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.LateStart,
                SeasonEvaluationAttentionSeverity.Warning,
                startVarianceDays.Value);
        }

        if (harvestVarianceDays > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.LateHarvest,
                SeasonEvaluationAttentionSeverity.Warning,
                harvestVarianceDays.Value);
        }

        if (input.PendingActivityCount > 0 &&
            input.CropCycleStatus is
                CropCycleStatus.Completed or
                CropCycleStatus.Cancelled)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.ActivitiesIncomplete,
                SeasonEvaluationAttentionSeverity.Warning,
                input.PendingActivityCount);
        }

        if (input.CancelledActivityCount > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.ActivitiesCancelled,
                SeasonEvaluationAttentionSeverity.Warning,
                input.CancelledActivityCount);
        }

        if (input.IssueActivityCount > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.ActivityIssuesRecorded,
                SeasonEvaluationAttentionSeverity.Warning,
                input.IssueActivityCount);
        }

        if (input.SopDeviatedActivityCount > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.SopDeviationRecorded,
                SeasonEvaluationAttentionSeverity.Warning,
                input.SopDeviatedActivityCount);
        }

        if (input.SopNotEvaluatedActivityCount > 0 &&
            input.CropCycleStatus is
                CropCycleStatus.Completed or
                CropCycleStatus.Cancelled)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.SopNotEvaluated,
                SeasonEvaluationAttentionSeverity.Warning,
                input.SopNotEvaluatedActivityCount);
        }

        if (input.CropCycleStatus == CropCycleStatus.Completed &&
            input.ConfirmedHarvestBatchCount == 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.NoConfirmedHarvest,
                SeasonEvaluationAttentionSeverity.Critical);
        }

        if (outstandingReceivable > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.OutstandingReceivable,
                SeasonEvaluationAttentionSeverity.Warning,
                outstandingReceivable);
        }

        if (profitabilityOutcome == ProfitabilityOutcome.BreakEven)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.BreakEven,
                SeasonEvaluationAttentionSeverity.Information);
        }
        else if (profitabilityOutcome == ProfitabilityOutcome.Loss)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.Loss,
                SeasonEvaluationAttentionSeverity.Critical,
                Math.Abs(netProfit));
        }

        if (capitalFundingGap > 0)
        {
            Add(
                attentions,
                SeasonEvaluationAttentionCode.CapitalFundingGap,
                SeasonEvaluationAttentionSeverity.Warning,
                capitalFundingGap);
        }

        return attentions
            .OrderByDescending(attention => attention.Severity)
            .ThenBy(attention => attention.Code)
            .ToArray();
    }

    private static void Add(
        ICollection<SeasonEvaluationAttention> attentions,
        SeasonEvaluationAttentionCode code,
        SeasonEvaluationAttentionSeverity severity,
        decimal? value = null)
    {
        attentions.Add(
            new SeasonEvaluationAttention(
                code,
                severity,
                value));
    }

    private static decimal? CalculatePercentage(
        int numerator,
        int denominator)
    {
        return denominator == 0
            ? null
            : Math.Round(
                (decimal)numerator / denominator * 100,
                4,
                MidpointRounding.AwayFromZero);
    }

    private static ProfitabilityOutcome
        DetermineProfitabilityOutcome(decimal netProfit)
    {
        if (netProfit < 0)
        {
            return ProfitabilityOutcome.Loss;
        }

        if (netProfit > 0)
        {
            return ProfitabilityOutcome.Profit;
        }

        return ProfitabilityOutcome.BreakEven;
    }

    private static void ValidateDates(
        SeasonEvaluationInput input)
    {
        if (input.PlannedStartDate == default)
        {
            throw new ArgumentException(
                "Planned start date must be provided.",
                nameof(input.PlannedStartDate));
        }

        if (input.ExpectedHarvestDate == default)
        {
            throw new ArgumentException(
                "Expected harvest date must be provided.",
                nameof(input.ExpectedHarvestDate));
        }

        if (input.ExpectedHarvestDate <= input.PlannedStartDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.ExpectedHarvestDate),
                "Expected harvest date must be after planned start date.");
        }

        switch (input.CropCycleStatus)
        {
            case CropCycleStatus.Planned
                when input.ActualStartDate.HasValue ||
                    input.ActualHarvestDate.HasValue:
                throw new ArgumentException(
                    "A planned cycle cannot have actual dates.");

            case CropCycleStatus.InProgress
                when !input.ActualStartDate.HasValue ||
                    input.ActualHarvestDate.HasValue:
                throw new ArgumentException(
                    "An in-progress cycle must have only an actual start date.");

            case CropCycleStatus.Completed
                when !input.ActualStartDate.HasValue ||
                    !input.ActualHarvestDate.HasValue:
                throw new ArgumentException(
                    "A completed cycle must have actual start and harvest dates.");

            case CropCycleStatus.Cancelled
                when input.ActualHarvestDate.HasValue:
                throw new ArgumentException(
                    "A cancelled cycle cannot have an actual harvest date.");
        }

        if (input.ActualStartDate.HasValue &&
            input.ActualHarvestDate.HasValue &&
            input.ActualHarvestDate.Value <
                input.ActualStartDate.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.ActualHarvestDate),
                "Actual harvest date cannot be before actual start date.");
        }
    }

    private static void ValidateActivityCounts(
        SeasonEvaluationInput input)
    {
        ValidateCount(
            input.TotalActivityCount,
            nameof(input.TotalActivityCount));

        ValidateCount(
            input.CompletedActivityCount,
            nameof(input.CompletedActivityCount));

        ValidateCount(
            input.CancelledActivityCount,
            nameof(input.CancelledActivityCount));

        ValidateCount(
            input.PendingActivityCount,
            nameof(input.PendingActivityCount));

        ValidateCount(
            input.IssueActivityCount,
            nameof(input.IssueActivityCount));

        if (input.CompletedActivityCount +
            input.CancelledActivityCount +
            input.PendingActivityCount !=
            input.TotalActivityCount)
        {
            throw new ArgumentException(
                "Activity status counts must equal total activity count.");
        }

        if (input.IssueActivityCount > input.TotalActivityCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.IssueActivityCount),
                "Issue activity count cannot exceed total activity count.");
        }
    }

    private static void ValidateSopCounts(
        SeasonEvaluationInput input)
    {
        ValidateCount(
            input.SopLinkedActivityCount,
            nameof(input.SopLinkedActivityCount));

        ValidateCount(
            input.SopCompliantActivityCount,
            nameof(input.SopCompliantActivityCount));

        ValidateCount(
            input.SopDeviatedActivityCount,
            nameof(input.SopDeviatedActivityCount));

        ValidateCount(
            input.SopNotEvaluatedActivityCount,
            nameof(input.SopNotEvaluatedActivityCount));

        if (input.SopLinkedActivityCount >
            input.TotalActivityCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.SopLinkedActivityCount),
                "SOP-linked activity count cannot exceed total activity count.");
        }

        if (input.SopCompliantActivityCount +
            input.SopDeviatedActivityCount +
            input.SopNotEvaluatedActivityCount !=
            input.SopLinkedActivityCount)
        {
            throw new ArgumentException(
                "SOP status counts must equal SOP-linked activity count.");
        }
    }

    private static void ValidateCount(
        int value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Count cannot be negative.");
        }
    }

    private static decimal NormalizeMoney(
        decimal value,
        string parameterName)
    {
        var normalized = Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);

        if (normalized < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Money cannot be negative.");
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value cannot be blank.",
                parameterName);
        }

        return value.Trim();
    }

    private static DateTime NormalizeGeneratedAt(
        DateTime value)
    {
        if (value == default)
        {
            throw new ArgumentException(
                "Generated-at cannot be default.",
                nameof(value));
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static void ValidateStatus(
        CropCycleStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "Crop cycle status is not supported.");
        }
    }

    private static void ValidateIdentifier(
        Guid value,
        string parameterName,
        string displayName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                $"{displayName} identifier cannot be empty.",
                parameterName);
        }
    }
}
