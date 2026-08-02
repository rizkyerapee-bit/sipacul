using SiPacul.Domain.Entities.Finance.Profitability;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.Profitability;

public sealed class RevenueAllocationCalculatorTests
{
    private static readonly Guid CycleA =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CycleB =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000002");

    [Fact]
    public void Allocate_OneLineWithoutDiscount_ShouldMatchSale()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                1000,
                0,
                400,
                new[]
                {
                    Line(1, CycleA, 1000)
                });

        var line = Assert.Single(result.Lines);

        Assert.Equal(1000m, line.NetRecognizedRevenue);
        Assert.Equal(400m, line.AllocatedCollectedRevenue);
        Assert.Equal(600m, line.OutstandingReceivable);
        Assert.Equal(1000m, result.RecognizedRevenue);
        Assert.Equal(400m, result.CollectedRevenue);
    }

    [Fact]
    public void Allocate_FullPayment_ShouldFullyCollectEveryLine()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                1000,
                100,
                900,
                new[]
                {
                    Line(1, CycleA, 600),
                    Line(2, CycleB, 400)
                });

        Assert.Equal(900m, result.RecognizedRevenue);
        Assert.Equal(900m, result.CollectedRevenue);
        Assert.Equal(0m, result.OutstandingReceivable);

        Assert.All(
            result.Lines,
            line =>
                Assert.Equal(
                    line.NetRecognizedRevenue,
                    line.AllocatedCollectedRevenue));
    }

    [Fact]
    public void Allocate_HeaderDiscount_ShouldBeProportional()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                1000,
                100,
                0,
                new[]
                {
                    Line(1, CycleA, 600),
                    Line(2, CycleB, 400)
                });

        Assert.Collection(
            result.Lines,
            first =>
            {
                Assert.Equal(
                    60m,
                    first.AllocatedSaleDiscount);

                Assert.Equal(
                    540m,
                    first.NetRecognizedRevenue);
            },
            second =>
            {
                Assert.Equal(
                    40m,
                    second.AllocatedSaleDiscount);

                Assert.Equal(
                    360m,
                    second.NetRecognizedRevenue);
            });
    }

    [Fact]
    public void Allocate_DiscountRemainder_ShouldGoToLastLineId()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                3,
                0.01m,
                0,
                new[]
                {
                    Line(3, CycleA, 1),
                    Line(1, CycleA, 1),
                    Line(2, CycleA, 1)
                });

        Assert.Equal(
            GuidForLine(1),
            result.Lines[0].SaleLineId);

        Assert.Equal(
            GuidForLine(3),
            result.Lines[2].SaleLineId);

        Assert.Equal(
            0.01m,
            result.Lines.Sum(line =>
                line.AllocatedSaleDiscount));

        Assert.Equal(
            0.01m,
            result.Lines[2].AllocatedSaleDiscount);
    }

    [Fact]
    public void Allocate_PartialPayment_ShouldBeProportional()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                1000,
                100,
                450,
                new[]
                {
                    Line(1, CycleA, 600),
                    Line(2, CycleB, 400)
                });

        Assert.Collection(
            result.Lines,
            first =>
                Assert.Equal(
                    270m,
                    first.AllocatedCollectedRevenue),
            second =>
                Assert.Equal(
                    180m,
                    second.AllocatedCollectedRevenue));
    }

    [Fact]
    public void Allocate_PaymentRemainder_ShouldGoToLastLineId()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                3,
                0,
                0.01m,
                new[]
                {
                    Line(3, CycleA, 1),
                    Line(1, CycleA, 1),
                    Line(2, CycleA, 1)
                });

        Assert.Equal(
            0.01m,
            result.Lines.Sum(line =>
                line.AllocatedCollectedRevenue));

        Assert.Equal(
            0.01m,
            result.Lines[2].AllocatedCollectedRevenue);
    }

    [Fact]
    public void AggregateByCropCycle_ShouldCombineCrossSaleLines()
    {
        var allocations =
            new[]
            {
                new SaleLineRevenueAllocation(
                    GuidForLine(1),
                    CycleA,
                    600,
                    60,
                    540,
                    270),
                new SaleLineRevenueAllocation(
                    GuidForLine(2),
                    CycleB,
                    400,
                    40,
                    360,
                    180),
                new SaleLineRevenueAllocation(
                    GuidForLine(3),
                    CycleA,
                    100,
                    0,
                    100,
                    50)
            };

        var result =
            RevenueAllocationCalculator
                .AggregateByCropCycle(allocations);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(CycleA, first.CropCycleId);
                Assert.Equal(640m, first.RecognizedRevenue);
                Assert.Equal(320m, first.CollectedRevenue);
                Assert.Equal(320m, first.OutstandingReceivable);
            },
            second =>
            {
                Assert.Equal(CycleB, second.CropCycleId);
                Assert.Equal(360m, second.RecognizedRevenue);
                Assert.Equal(180m, second.CollectedRevenue);
                Assert.Equal(180m, second.OutstandingReceivable);
            });
    }

    [Fact]
    public void Allocate_ShouldRoundMoneyAwayFromZero()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                10.005m,
                0,
                5.005m,
                new[]
                {
                    Line(1, CycleA, 10.005m)
                });

        Assert.Equal(10.01m, result.Subtotal);
        Assert.Equal(5.01m, result.ConfirmedPaymentAmount);
    }

    [Fact]
    public void Allocate_FullDiscountWithNoPayment_ShouldAllowZeroRevenue()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                100,
                100,
                0,
                new[]
                {
                    Line(1, CycleA, 100)
                });

        Assert.Equal(0m, result.SaleTotalAmount);
        Assert.Equal(0m, result.RecognizedRevenue);
        Assert.Equal(0m, result.CollectedRevenue);
    }

    [Fact]
    public void Allocate_ZeroValueSale_ShouldRemainZero()
    {
        var result =
            RevenueAllocationCalculator.Allocate(
                0,
                0,
                0,
                new[]
                {
                    Line(1, CycleA, 0)
                });

        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.SaleTotalAmount);
        Assert.Equal(0m, result.RecognizedRevenue);
        Assert.Equal(0m, result.CollectedRevenue);
        Assert.Equal(0m, result.OutstandingReceivable);
    }

    [Fact]
    public void Allocate_EmptyLines_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            RevenueAllocationCalculator.Allocate(
                100,
                0,
                0,
                Array.Empty<SaleRevenueLineInput>()));
    }

    [Fact]
    public void Allocate_DuplicateLineIdentifiers_ShouldThrow()
    {
        var duplicateId = GuidForLine(1);

        Assert.Throws<ArgumentException>(() =>
            RevenueAllocationCalculator.Allocate(
                200,
                0,
                0,
                new[]
                {
                    new SaleRevenueLineInput(
                        duplicateId,
                        CycleA,
                        100),
                    new SaleRevenueLineInput(
                        duplicateId,
                        CycleB,
                        100)
                }));
    }

    [Fact]
    public void Allocate_EmptyLineIdentifier_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            RevenueAllocationCalculator.Allocate(
                100,
                0,
                0,
                new[]
                {
                    new SaleRevenueLineInput(
                        Guid.Empty,
                        CycleA,
                        100)
                }));
    }

    [Fact]
    public void Allocate_EmptyCropCycleIdentifier_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            RevenueAllocationCalculator.Allocate(
                100,
                0,
                0,
                new[]
                {
                    new SaleRevenueLineInput(
                        GuidForLine(1),
                        Guid.Empty,
                        100)
                }));
    }

    [Fact]
    public void Allocate_NegativeLineTotal_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RevenueAllocationCalculator.Allocate(
                100,
                0,
                0,
                new[]
                {
                    Line(1, CycleA, -100)
                }));
    }

    [Fact]
    public void Allocate_SubtotalMismatch_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            RevenueAllocationCalculator.Allocate(
                200,
                0,
                0,
                new[]
                {
                    Line(1, CycleA, 100)
                }));
    }

    [Fact]
    public void Allocate_DiscountAboveSubtotal_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RevenueAllocationCalculator.Allocate(
                100,
                100.01m,
                0,
                new[]
                {
                    Line(1, CycleA, 100)
                }));
    }

    [Fact]
    public void Allocate_PaymentAboveSaleTotal_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RevenueAllocationCalculator.Allocate(
                100,
                10,
                90.01m,
                new[]
                {
                    Line(1, CycleA, 100)
                }));
    }

    private static SaleRevenueLineInput Line(
        int sequence,
        Guid cropCycleId,
        decimal lineTotal)
    {
        return new SaleRevenueLineInput(
            GuidForLine(sequence),
            cropCycleId,
            lineTotal);
    }

    private static Guid GuidForLine(int sequence)
    {
        return Guid.Parse(
            $"20000000-0000-0000-0000-" +
            $"{sequence:000000000000}");
    }
}
