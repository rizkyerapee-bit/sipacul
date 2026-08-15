using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Domain.Entities.Finance.Profitability;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Evaluations;

public sealed class SeasonEvaluationCalculatorTests
{
    [Fact]
    public void Calculate_HealthyCompletedSeason_ShouldNeedNoAttention()
    {
        var report =
            SeasonEvaluationCalculator.Calculate(Input());

        Assert.True(report.IsReadyForReview);
        Assert.False(report.RequiresAttention);
        Assert.Empty(report.Attentions);
        Assert.Equal(1000m, report.NetProfit);
        Assert.Equal(50m, report.ProfitMarginPercentage);
        Assert.Equal(
            ProfitabilityOutcome.Profit,
            report.ProfitabilityOutcome);
    }

    [Fact]
    public void Calculate_NonTerminalCycle_ShouldFlagNotReady()
    {
        var input =
            Input() with
            {
                CropCycleStatus = CropCycleStatus.Planned,
                ActualStartDate = null,
                ActualHarvestDate = null
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.False(report.IsReadyForReview);
        Assert.True(report.RequiresAttention);
        AssertAttention(
            report,
            SeasonEvaluationAttentionCode.CycleNotTerminal,
            SeasonEvaluationAttentionSeverity.Warning);
    }

    [Fact]
    public void Calculate_CancelledCycle_ShouldFlagCritical()
    {
        var input =
            Input() with
            {
                CropCycleStatus = CropCycleStatus.Cancelled,
                ActualHarvestDate = null,
                ConfirmedHarvestBatchCount = 0
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.True(report.IsReadyForReview);
        Assert.Equal(1, report.CriticalAttentionCount);
        AssertAttention(
            report,
            SeasonEvaluationAttentionCode.CycleCancelled,
            SeasonEvaluationAttentionSeverity.Critical);

        Assert.DoesNotContain(
            report.Attentions,
            attention =>
                attention.Code ==
                    SeasonEvaluationAttentionCode.NoConfirmedHarvest);
    }

    [Fact]
    public void Calculate_LateDates_ShouldExposeDayVariances()
    {
        var input =
            Input() with
            {
                ActualStartDate = new DateOnly(2027, 2, 4),
                ActualHarvestDate = new DateOnly(2027, 5, 6)
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.Equal(3, report.StartVarianceDays);
        Assert.Equal(5, report.HarvestVarianceDays);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.LateStart,
            3);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.LateHarvest,
            5);
    }

    [Fact]
    public void Calculate_ActivityProblems_ShouldCreateSeparateFlags()
    {
        var input =
            Input() with
            {
                TotalActivityCount = 10,
                CompletedActivityCount = 6,
                CancelledActivityCount = 2,
                PendingActivityCount = 2,
                IssueActivityCount = 3,
                SopLinkedActivityCount = 0,
                SopCompliantActivityCount = 0
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.Equal(60m, report.ActivityCompletionPercentage);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.ActivitiesIncomplete,
            2);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.ActivitiesCancelled,
            2);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.ActivityIssuesRecorded,
            3);
    }

    [Fact]
    public void Calculate_SopProblems_ShouldCreateSeparateFlags()
    {
        var input =
            Input() with
            {
                SopLinkedActivityCount = 10,
                SopCompliantActivityCount = 6,
                SopDeviatedActivityCount = 3,
                SopNotEvaluatedActivityCount = 1
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.Equal(60m, report.SopCompliancePercentage);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.SopDeviationRecorded,
            3);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.SopNotEvaluated,
            1);
    }

    [Fact]
    public void Calculate_CompletedWithoutHarvest_ShouldFlagCritical()
    {
        var input = Input() with
        {
            ConfirmedHarvestBatchCount = 0
        };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        AssertAttention(
            report,
            SeasonEvaluationAttentionCode.NoConfirmedHarvest,
            SeasonEvaluationAttentionSeverity.Critical);
    }

    [Fact]
    public void Calculate_FinancialRisks_ShouldReconcileAndFlag()
    {
        var input =
            Input() with
            {
                RecognizedRevenue = 800,
                CollectedRevenue = 500,
                TotalCultivationCost = 1000,
                CapitalFundingGap = 100
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.Equal(300m, report.OutstandingReceivable);
        Assert.Equal(-200m, report.NetProfit);
        Assert.Equal(-25m, report.ProfitMarginPercentage);
        Assert.Equal(
            ProfitabilityOutcome.Loss,
            report.ProfitabilityOutcome);
        Assert.Equal(1, report.CriticalAttentionCount);
        Assert.Equal(2, report.WarningAttentionCount);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.OutstandingReceivable,
            300);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.Loss,
            200);
        AssertAttentionValue(
            report,
            SeasonEvaluationAttentionCode.CapitalFundingGap,
            100);
    }

    [Fact]
    public void Calculate_BreakEven_ShouldBeInformationalOnly()
    {
        var input =
            Input() with
            {
                RecognizedRevenue = 1000,
                CollectedRevenue = 1000,
                TotalCultivationCost = 1000
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.False(report.RequiresAttention);
        Assert.Equal(1, report.InformationAttentionCount);
        AssertAttention(
            report,
            SeasonEvaluationAttentionCode.BreakEven,
            SeasonEvaluationAttentionSeverity.Information);
    }

    [Fact]
    public void Calculate_ZeroActivityCounts_ShouldReturnNullPercentages()
    {
        var input =
            Input() with
            {
                TotalActivityCount = 0,
                CompletedActivityCount = 0,
                SopLinkedActivityCount = 0,
                SopCompliantActivityCount = 0
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.Null(report.ActivityCompletionPercentage);
        Assert.Null(report.SopCompliancePercentage);
    }

    [Fact]
    public void Calculate_ShouldNormalizeSnapshotsMoneyAndTimestamp()
    {
        var input =
            Input() with
            {
                CropCycleCode = "  SB-001  ",
                LandName = "  Lahan Utama  ",
                CommodityName = "  Padi  ",
                RecognizedRevenue = 2000.005m,
                CollectedRevenue = 2000.005m,
                TotalCultivationCost = 1000.005m,
                GeneratedAt = new DateTime(
                    2027,
                    5,
                    7,
                    10,
                    0,
                    0,
                    DateTimeKind.Unspecified)
            };

        var report =
            SeasonEvaluationCalculator.Calculate(input);

        Assert.Equal("SB-001", report.CropCycleCode);
        Assert.Equal("Lahan Utama", report.LandName);
        Assert.Equal("Padi", report.CommodityName);
        Assert.Equal(2000.01m, report.RecognizedRevenue);
        Assert.Equal(1000.01m, report.TotalCultivationCost);
        Assert.Equal(1000m, report.NetProfit);
        Assert.Equal(DateTimeKind.Utc, report.GeneratedAt.Kind);
    }

    [Fact]
    public void Calculate_EmptyIdentifier_ShouldThrow()
    {
        var input = Input() with
        {
            OrganizationId = Guid.Empty
        };

        Assert.Throws<ArgumentException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_BlankSnapshot_ShouldThrow()
    {
        var input = Input() with
        {
            LandCode = " "
        };

        Assert.Throws<ArgumentException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_InvalidLifecycleDates_ShouldThrow()
    {
        var input =
            Input() with
            {
                CropCycleStatus = CropCycleStatus.InProgress,
                ActualStartDate = null,
                ActualHarvestDate = null
            };

        Assert.Throws<ArgumentException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_InvalidActivityTotals_ShouldThrow()
    {
        var input =
            Input() with
            {
                TotalActivityCount = 10,
                CompletedActivityCount = 8,
                CancelledActivityCount = 1,
                PendingActivityCount = 0
            };

        Assert.Throws<ArgumentException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_InvalidSopTotals_ShouldThrow()
    {
        var input =
            Input() with
            {
                SopLinkedActivityCount = 10,
                SopCompliantActivityCount = 8,
                SopDeviatedActivityCount = 1,
                SopNotEvaluatedActivityCount = 0
            };

        Assert.Throws<ArgumentException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_CollectedAboveRevenue_ShouldThrow()
    {
        var input =
            Input() with
            {
                RecognizedRevenue = 1000,
                CollectedRevenue = 1001
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_NegativeCount_ShouldThrow()
    {
        var input =
            Input() with
            {
                TotalActivityCount = -1,
                CompletedActivityCount = -1,
                SopLinkedActivityCount = 0,
                SopCompliantActivityCount = 0
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SeasonEvaluationCalculator.Calculate(input));
    }

    private static SeasonEvaluationInput Input()
    {
        return new SeasonEvaluationInput(
            OrganizationId: Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            CropCycleId: Guid.Parse(
                "22222222-2222-2222-2222-222222222222"),
            CropCycleCode: "SB-001",
            CropCycleName: "Musim Padi Pertama",
            LandId: Guid.Parse(
                "33333333-3333-3333-3333-333333333333"),
            LandCode: "LH-001",
            LandName: "Lahan Utama",
            LandPlotId: Guid.Parse(
                "44444444-4444-4444-4444-444444444444"),
            LandPlotCode: "PTK-001",
            LandPlotName: "Petak A",
            CommodityId: Guid.Parse(
                "55555555-5555-5555-5555-555555555555"),
            CommodityCode: "PADI",
            CommodityName: "Padi",
            CropCycleStatus: CropCycleStatus.Completed,
            PlannedStartDate: new DateOnly(2027, 2, 1),
            ExpectedHarvestDate: new DateOnly(2027, 5, 1),
            ActualStartDate: new DateOnly(2027, 2, 1),
            ActualHarvestDate: new DateOnly(2027, 5, 1),
            TotalActivityCount: 10,
            CompletedActivityCount: 10,
            CancelledActivityCount: 0,
            PendingActivityCount: 0,
            IssueActivityCount: 0,
            SopLinkedActivityCount: 10,
            SopCompliantActivityCount: 10,
            SopDeviatedActivityCount: 0,
            SopNotEvaluatedActivityCount: 0,
            ConfirmedHarvestBatchCount: 1,
            RecognizedRevenue: 2000,
            CollectedRevenue: 2000,
            TotalCultivationCost: 1000,
            CapitalFundingGap: 0,
            GeneratedAt: new DateTime(
                2027,
                5,
                7,
                10,
                0,
                0,
                DateTimeKind.Utc));
    }

    private static void AssertAttention(
        SeasonEvaluationReport report,
        SeasonEvaluationAttentionCode code,
        SeasonEvaluationAttentionSeverity severity)
    {
        Assert.Contains(
            report.Attentions,
            attention =>
                attention.Code == code &&
                attention.Severity == severity);
    }

    private static void AssertAttentionValue(
        SeasonEvaluationReport report,
        SeasonEvaluationAttentionCode code,
        decimal value)
    {
        var attention = Assert.Single(
            report.Attentions,
            item => item.Code == code);

        Assert.Equal(value, attention.Value);
    }
}
