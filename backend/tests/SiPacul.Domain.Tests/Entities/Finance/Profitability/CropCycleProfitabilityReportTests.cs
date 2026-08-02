using SiPacul.Domain.Entities.Finance.Profitability;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.Profitability;

public sealed class CropCycleProfitabilityReportTests
{
    [Fact]
    public void Calculate_Profit_ShouldCalculateAllTotals()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    recognizedRevenue: 2000000,
                    collectedRevenue: 1500000,
                    activityCost: 600000,
                    manualCost: 400000,
                    investorCapital: 700000,
                    partnerCapital: 300000));

        Assert.Equal(2000000m, report.RecognizedRevenue);
        Assert.Equal(1500000m, report.CollectedRevenue);
        Assert.Equal(500000m, report.OutstandingReceivable);
        Assert.Equal(1000000m, report.TotalCultivationCost);
        Assert.Equal(1000000m, report.NetProfit);
        Assert.Equal(50m, report.ProfitMarginPercentage);

        Assert.Equal(
            ProfitabilityOutcome.Profit,
            report.Outcome);

        Assert.Equal(1000000m, report.TotalConfirmedCapital);
        Assert.Equal(0m, report.CapitalFundingGap);
        Assert.Equal(0m, report.CapitalFundingExcess);
    }

    [Fact]
    public void Calculate_BreakEven_ShouldReturnBreakEven()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    recognizedRevenue: 1000000,
                    collectedRevenue: 1000000,
                    activityCost: 600000,
                    manualCost: 400000));

        Assert.Equal(0m, report.NetProfit);

        Assert.Equal(
            ProfitabilityOutcome.BreakEven,
            report.Outcome);

        Assert.Equal(0m, report.ProfitMarginPercentage);
    }

    [Fact]
    public void Calculate_Loss_ShouldReturnNegativeProfit()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    recognizedRevenue: 600000,
                    collectedRevenue: 500000,
                    activityCost: 700000,
                    manualCost: 300000));

        Assert.Equal(-400000m, report.NetProfit);

        Assert.Equal(
            -66.6667m,
            report.ProfitMarginPercentage);

        Assert.Equal(
            ProfitabilityOutcome.Loss,
            report.Outcome);
    }

    [Fact]
    public void Calculate_ZeroRevenue_ShouldReturnNullMargin()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    recognizedRevenue: 0,
                    collectedRevenue: 0,
                    activityCost: 100000,
                    manualCost: 0));

        Assert.Null(report.ProfitMarginPercentage);
        Assert.Equal(-100000m, report.NetProfit);

        Assert.Equal(
            ProfitabilityOutcome.Loss,
            report.Outcome);
    }

    [Fact]
    public void Calculate_Underfunded_ShouldCalculateGap()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    activityCost: 600000,
                    manualCost: 400000,
                    investorCapital: 500000,
                    partnerCapital: 100000));

        Assert.Equal(400000m, report.CapitalFundingGap);
        Assert.Equal(0m, report.CapitalFundingExcess);
    }

    [Fact]
    public void Calculate_Overfunded_ShouldCalculateExcess()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    activityCost: 600000,
                    manualCost: 400000,
                    investorCapital: 1000000,
                    partnerCapital: 250000));

        Assert.Equal(0m, report.CapitalFundingGap);
        Assert.Equal(250000m, report.CapitalFundingExcess);
    }

    [Fact]
    public void Calculate_ShouldRoundMoneyAwayFromZero()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    recognizedRevenue: 100.005m,
                    collectedRevenue: 50.005m,
                    activityCost: 10.005m,
                    manualCost: 20.005m,
                    investorCapital: 15.005m,
                    partnerCapital: 15.005m));

        Assert.Equal(100.01m, report.RecognizedRevenue);
        Assert.Equal(50.01m, report.CollectedRevenue);
        Assert.Equal(30.02m, report.TotalCultivationCost);
        Assert.Equal(69.99m, report.NetProfit);
        Assert.Equal(30.02m, report.TotalConfirmedCapital);
    }

    [Fact]
    public void Calculate_ShouldRoundQuantityToFourDecimals()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                Input(
                    availableHarvestQuantity:
                        12.34565m));

        Assert.Equal(
            12.3457m,
            report.AvailableHarvestQuantity);
    }

    [Fact]
    public void Calculate_ShouldTrimSnapshots()
    {
        var input =
            Input() with
            {
                CropCycleCode = "  CC-001  ",
                CropCycleName = "  Musim Padi  ",
                CommodityCodeSnapshot = "  PADI  ",
                CommodityNameSnapshot = "  Padi  "
            };

        var report =
            CropCycleProfitabilityReport.Calculate(input);

        Assert.Equal("CC-001", report.CropCycleCode);
        Assert.Equal("Musim Padi", report.CropCycleName);
        Assert.Equal("PADI", report.CommodityCodeSnapshot);
        Assert.Equal("Padi", report.CommodityNameSnapshot);
    }

    [Fact]
    public void Calculate_UnspecifiedGeneratedAt_ShouldBecomeUtc()
    {
        var input =
            Input() with
            {
                GeneratedAt = new DateTime(
                    2027,
                    1,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Unspecified)
            };

        var report =
            CropCycleProfitabilityReport.Calculate(input);

        Assert.Equal(
            DateTimeKind.Utc,
            report.GeneratedAt.Kind);
    }

    [Fact]
    public void Calculate_CollectedAboveRecognized_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input(
                    recognizedRevenue: 100,
                    collectedRevenue: 100.01m)));
    }

    [Fact]
    public void Calculate_NegativeMoney_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input(
                    activityCost: -1)));
    }

    [Fact]
    public void Calculate_NegativeHarvestQuantity_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input(
                    availableHarvestQuantity: -1)));
    }

    [Fact]
    public void Calculate_EmptyOrganizationId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input() with
                {
                    OrganizationId = Guid.Empty
                }));
    }

    [Fact]
    public void Calculate_EmptyCropCycleId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input() with
                {
                    CropCycleId = Guid.Empty
                }));
    }

    [Fact]
    public void Calculate_BlankSnapshot_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input() with
                {
                    CommodityNameSnapshot = " "
                }));
    }

    [Fact]
    public void Calculate_DefaultGeneratedAt_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CropCycleProfitabilityReport.Calculate(
                Input() with
                {
                    GeneratedAt = default
                }));
    }

    private static CropCycleProfitabilityInput Input(
        decimal recognizedRevenue = 1000000,
        decimal collectedRevenue = 500000,
        decimal activityCost = 300000,
        decimal manualCost = 200000,
        decimal investorCapital = 400000,
        decimal partnerCapital = 100000,
        decimal availableHarvestQuantity = 50)
    {
        return new CropCycleProfitabilityInput(
            Guid.Parse(
                "30000000-0000-0000-0000-000000000001"),
            Guid.Parse(
                "40000000-0000-0000-0000-000000000001"),
            "CC-001",
            "Musim Padi",
            Guid.Parse(
                "50000000-0000-0000-0000-000000000001"),
            "PADI",
            "Padi",
            recognizedRevenue,
            collectedRevenue,
            activityCost,
            manualCost,
            investorCapital,
            partnerCapital,
            availableHarvestQuantity,
            new DateTime(
                2027,
                7,
                1,
                8,
                0,
                0,
                DateTimeKind.Utc));
    }
}
