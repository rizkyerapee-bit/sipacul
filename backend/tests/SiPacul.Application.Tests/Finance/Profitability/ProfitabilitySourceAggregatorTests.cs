using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;
using Xunit;

namespace SiPacul.Application.Tests.Finance.Profitability;

public sealed class ProfitabilitySourceAggregatorTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid OtherCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000002");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Fact]
    public void Build_PlannedActivity_ShouldExcludeResourceCost()
    {
        var snapshot =
            Build(
                activityResources:
                [
                    new(
                        CultivationActivityStatus.Planned,
                        null,
                        100)
                ]);

        Assert.Equal(0m, snapshot.ActivityResourceCost);
    }

    [Fact]
    public void Build_InProgressActivity_ShouldIncludeResourceCost()
    {
        var snapshot =
            Build(
                activityResources:
                [
                    new(
                        CultivationActivityStatus.InProgress,
                        new DateOnly(2027, 1, 1),
                        100.005m)
                ]);

        Assert.Equal(
            100.01m,
            snapshot.ActivityResourceCost);
    }

    [Fact]
    public void Build_CompletedActivity_ShouldIncludeResourceCost()
    {
        var snapshot =
            Build(
                activityResources:
                [
                    new(
                        CultivationActivityStatus.Completed,
                        new DateOnly(2027, 1, 1),
                        125)
                ]);

        Assert.Equal(
            125m,
            snapshot.ActivityResourceCost);
    }

    [Fact]
    public void Build_CancelledStartedActivity_ShouldIncludeCost()
    {
        var snapshot =
            Build(
                activityResources:
                [
                    new(
                        CultivationActivityStatus.Cancelled,
                        new DateOnly(2027, 1, 1),
                        150)
                ]);

        Assert.Equal(
            150m,
            snapshot.ActivityResourceCost);
    }

    [Fact]
    public void Build_CancelledUnstartedActivity_ShouldExcludeCost()
    {
        var snapshot =
            Build(
                activityResources:
                [
                    new(
                        CultivationActivityStatus.Cancelled,
                        null,
                        150)
                ]);

        Assert.Equal(0m, snapshot.ActivityResourceCost);
    }

    [Fact]
    public void Build_ManualExpense_ShouldOnlyCountConfirmed()
    {
        var snapshot =
            Build(
                manualExpenses:
                [
                    new(
                        CultivationExpenseStatus.Draft,
                        100),
                    new(
                        CultivationExpenseStatus.Confirmed,
                        200),
                    new(
                        CultivationExpenseStatus.Cancelled,
                        300)
                ]);

        Assert.Equal(
            200m,
            snapshot.ManualExpenseCost);
    }

    [Fact]
    public void Build_Capital_ShouldSplitConfirmedRoles()
    {
        var snapshot =
            Build(
                capital:
                [
                    new(
                        CapitalContributionStatus.Confirmed,
                        CapitalContributorRole.Investor,
                        700),
                    new(
                        CapitalContributionStatus.Confirmed,
                        CapitalContributorRole.Partner,
                        300),
                    new(
                        CapitalContributionStatus.Draft,
                        CapitalContributorRole.Investor,
                        500),
                    new(
                        CapitalContributionStatus.Cancelled,
                        CapitalContributorRole.Partner,
                        500)
                ]);

        Assert.Equal(
            700m,
            snapshot.ConfirmedInvestorCapital);

        Assert.Equal(
            300m,
            snapshot.ConfirmedPartnerCapital);
    }

    [Fact]
    public void Build_DraftAndCancelledSales_ShouldNotBeRevenue()
    {
        var snapshot =
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Draft,
                        1000,
                        0,
                        1000,
                        [
                            Line(1, CycleId, 1000, 10)
                        ]),
                    Sale(
                        2,
                        SaleStatus.Cancelled,
                        1000,
                        0,
                        1000,
                        [
                            Line(2, CycleId, 1000, 10)
                        ])
                ]);

        Assert.Equal(0m, snapshot.RecognizedRevenue);
        Assert.Equal(0m, snapshot.CollectedRevenue);
    }

    [Fact]
    public void Build_ConfirmedCrossCycleSale_ShouldAllocateRevenue()
    {
        var snapshot =
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Confirmed,
                        1000,
                        100,
                        900,
                        [
                            Line(1, CycleId, 600, 6),
                            Line(2, OtherCycleId, 400, 4)
                        ],
                        [
                            Payment(
                                SalePaymentStatus.Confirmed,
                                450)
                        ])
                ]);

        Assert.Equal(540m, snapshot.RecognizedRevenue);
        Assert.Equal(270m, snapshot.CollectedRevenue);
    }

    [Fact]
    public void Build_MultipleConfirmedSales_ShouldAggregate()
    {
        var snapshot =
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Confirmed,
                        1000,
                        0,
                        1000,
                        [
                            Line(1, CycleId, 1000, 10)
                        ],
                        [
                            Payment(
                                SalePaymentStatus.Confirmed,
                                500)
                        ]),
                    Sale(
                        2,
                        SaleStatus.Confirmed,
                        200,
                        0,
                        200,
                        [
                            Line(2, CycleId, 200, 2)
                        ],
                        [
                            Payment(
                                SalePaymentStatus.Confirmed,
                                200)
                        ])
                ]);

        Assert.Equal(1200m, snapshot.RecognizedRevenue);
        Assert.Equal(700m, snapshot.CollectedRevenue);
    }

    [Fact]
    public void Build_Payments_ShouldOnlyCountConfirmed()
    {
        var snapshot =
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Confirmed,
                        1000,
                        0,
                        1000,
                        [
                            Line(1, CycleId, 1000, 10)
                        ],
                        [
                            Payment(
                                SalePaymentStatus.Draft,
                                300),
                            Payment(
                                SalePaymentStatus.Confirmed,
                                400),
                            Payment(
                                SalePaymentStatus.Cancelled,
                                300)
                        ])
                ]);

        Assert.Equal(400m, snapshot.CollectedRevenue);
    }

    [Fact]
    public void Build_Harvest_ShouldSubtractConfirmedSoldQuantity()
    {
        var snapshot =
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Confirmed,
                        1000,
                        0,
                        1000,
                        [
                            Line(1, CycleId, 1000, 25)
                        ])
                ],
                harvests:
                [
                    Harvest(
                        HarvestBatchStatus.Confirmed,
                        HarvestQuantityUnit.Kilogram,
                        100)
                ]);

        Assert.Equal(
            75m,
            snapshot.AvailableHarvestQuantity);

        Assert.Equal(
            HarvestQuantityUnit.Kilogram,
            snapshot.HarvestQuantityUnit);
    }

    [Fact]
    public void Build_CancelledSale_ShouldNotReduceHarvest()
    {
        var snapshot =
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Cancelled,
                        1000,
                        0,
                        1000,
                        [
                            Line(1, CycleId, 1000, 25)
                        ])
                ],
                harvests:
                [
                    Harvest(
                        HarvestBatchStatus.Confirmed,
                        HarvestQuantityUnit.Kilogram,
                        100)
                ]);

        Assert.Equal(
            100m,
            snapshot.AvailableHarvestQuantity);
    }

    [Fact]
    public void Build_DraftHarvest_ShouldNotBeAvailable()
    {
        var snapshot =
            Build(
                harvests:
                [
                    Harvest(
                        HarvestBatchStatus.Draft,
                        HarvestQuantityUnit.Kilogram,
                        100)
                ]);

        Assert.Equal(
            0m,
            snapshot.AvailableHarvestQuantity);

        Assert.Null(snapshot.HarvestQuantityUnit);
    }

    [Fact]
    public void Build_MixedConfirmedHarvestUnits_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(
                harvests:
                [
                    Harvest(
                        HarvestBatchStatus.Confirmed,
                        HarvestQuantityUnit.Kilogram,
                        100),
                    Harvest(
                        HarvestBatchStatus.Confirmed,
                        HarvestQuantityUnit.Ton,
                        1)
                ]));
    }

    [Fact]
    public void Build_SoldAboveHarvest_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Confirmed,
                        1000,
                        0,
                        1000,
                        [
                            Line(1, CycleId, 1000, 101)
                        ])
                ],
                harvests:
                [
                    Harvest(
                        HarvestBatchStatus.Confirmed,
                        HarvestQuantityUnit.Kilogram,
                        100)
                ]));
    }

    [Fact]
    public void Build_SaleTotalMismatch_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(
                sales:
                [
                    Sale(
                        1,
                        SaleStatus.Confirmed,
                        1000,
                        100,
                        800,
                        [
                            Line(1, CycleId, 1000, 10)
                        ])
                ]));
    }

    [Fact]
    public void Build_DuplicateSaleIdentifiers_ShouldThrow()
    {
        var sale =
            Sale(
                1,
                SaleStatus.Confirmed,
                100,
                0,
                100,
                [
                    Line(1, CycleId, 100, 1)
                ]);

        Assert.Throws<ArgumentException>(() =>
            Build(
                sales:
                [
                    sale,
                    sale
                ]));
    }

    [Fact]
    public void Build_NoSources_ShouldReturnZeroTotals()
    {
        var snapshot = Build();

        Assert.Equal(0m, snapshot.RecognizedRevenue);
        Assert.Equal(0m, snapshot.CollectedRevenue);
        Assert.Equal(0m, snapshot.ActivityResourceCost);
        Assert.Equal(0m, snapshot.ManualExpenseCost);
        Assert.Equal(0m, snapshot.ConfirmedInvestorCapital);
        Assert.Equal(0m, snapshot.ConfirmedPartnerCapital);
        Assert.Equal(0m, snapshot.AvailableHarvestQuantity);
    }

    [Fact]
    public void ToInput_ShouldMapSnapshotAndGeneratedAt()
    {
        var snapshot = Build();

        var generatedAt =
            new DateTime(
                2027,
                7,
                1,
                8,
                0,
                0,
                DateTimeKind.Utc);

        var input =
            snapshot.ToInput(generatedAt);

        Assert.Equal(
            OrganizationId,
            input.OrganizationId);

        Assert.Equal(CycleId, input.CropCycleId);
        Assert.Equal("CC-001", input.CropCycleCode);
        Assert.Equal("Musim Padi", input.CropCycleName);
        Assert.Equal(CommodityId, input.CommodityIdSnapshot);
        Assert.Equal("PADI", input.CommodityCodeSnapshot);
        Assert.Equal("Padi", input.CommodityNameSnapshot);
        Assert.Equal(generatedAt, input.GeneratedAt);
    }

    private static ProfitabilitySourceSnapshot Build(
        IReadOnlyCollection<ActivityResourceCostSource>?
            activityResources = null,
        IReadOnlyCollection<ManualExpenseSource>?
            manualExpenses = null,
        IReadOnlyCollection<CapitalContributionSource>?
            capital = null,
        IReadOnlyCollection<ProfitabilitySaleSource>?
            sales = null,
        IReadOnlyCollection<ProfitabilityHarvestSource>?
            harvests = null)
    {
        return ProfitabilitySourceAggregator.Build(
            OrganizationId,
            CycleId,
            "CC-001",
            "Musim Padi",
            CommodityId,
            "PADI",
            "Padi",
            activityResources ??
                Array.Empty<ActivityResourceCostSource>(),
            manualExpenses ??
                Array.Empty<ManualExpenseSource>(),
            capital ??
                Array.Empty<CapitalContributionSource>(),
            sales ??
                Array.Empty<ProfitabilitySaleSource>(),
            harvests ??
                Array.Empty<ProfitabilityHarvestSource>());
    }

    private static ProfitabilitySaleSource Sale(
        int sequence,
        SaleStatus status,
        decimal subtotal,
        decimal discount,
        decimal total,
        IReadOnlyList<ProfitabilitySaleLineSource> lines,
        IReadOnlyList<ProfitabilityPaymentSource>? payments =
            null)
    {
        return new ProfitabilitySaleSource(
            GuidFor("40000000", sequence),
            status,
            subtotal,
            discount,
            total,
            lines,
            payments ??
                Array.Empty<ProfitabilityPaymentSource>());
    }

    private static ProfitabilitySaleLineSource Line(
        int sequence,
        Guid cropCycleId,
        decimal lineTotal,
        decimal quantity)
    {
        return new ProfitabilitySaleLineSource(
            GuidFor("50000000", sequence),
            cropCycleId,
            lineTotal,
            quantity);
    }

    private static ProfitabilityPaymentSource Payment(
        SalePaymentStatus status,
        decimal amount)
    {
        return new ProfitabilityPaymentSource(
            status,
            amount);
    }

    private static ProfitabilityHarvestSource Harvest(
        HarvestBatchStatus status,
        HarvestQuantityUnit quantityUnit,
        decimal netQuantity)
    {
        return new ProfitabilityHarvestSource(
            status,
            quantityUnit,
            netQuantity);
    }

    private static Guid GuidFor(
        string prefix,
        int sequence)
    {
        return Guid.Parse(
            $"{prefix}-0000-0000-0000-" +
            $"{sequence:000000000000}");
    }
}
