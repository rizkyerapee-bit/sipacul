using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.ProfitSharing;

public sealed class ProfitSharingCalculatorTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Fact]
    public void Calculate_AllCapitalFromInvestor_ShouldUseTwoThirdsOneThird()
    {
        var result =
            Calculate(
                revenue: 600,
                cost: 300,
                investorCapital: 300,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        300)
                ]);

        var investor =
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-001");

        var partner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-001");

        Assert.Equal(100m, result.ManagementProfitPool);
        Assert.Equal(200m, result.CapitalProfitPool);
        Assert.Equal(200m, investor.TotalProfitShare);
        Assert.Equal(100m, partner.TotalProfitShare);
        Assert.Equal(500m, investor.TotalPayout);
        Assert.Equal(100m, partner.TotalPayout);
    }

    [Fact]
    public void Calculate_PartnerCapital_ShouldAddCapitalProfit()
    {
        var result =
            Calculate(
                revenue: 600,
                cost: 300,
                investorCapital: 200,
                partnerCapital: 100,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        200),
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        100)
                ]);

        var investor =
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-001");

        var partner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-001");

        Assert.Equal(133.33m, investor.CapitalProfitShare);
        Assert.Equal(66.67m, partner.CapitalProfitShare);
        Assert.Equal(100m, partner.ManagementProfitShare);
        Assert.Equal(166.67m, partner.TotalProfitShare);
        Assert.Equal(300m, result.NetProfit);
    }

    [Fact]
    public void Calculate_ManagingPartnerWithoutCapital_ShouldStillExist()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        var partner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-001");

        Assert.Equal(0m, partner.ConfirmedCapital);
        Assert.Equal(0m, partner.CapitalRatio);
        Assert.Equal(0m, partner.CapitalRecovery);
        Assert.Equal(0m, partner.CapitalLoss);
        Assert.Equal(16.67m, partner.ManagementProfitShare);
        Assert.Equal(0m, partner.CapitalProfitShare);
    }

    [Fact]
    public void Calculate_DuplicateContributions_ShouldGroupCapital()
    {
        var result =
            Calculate(
                revenue: 200,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        40),
                    Contributor(
                        "inv-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        60)
                ]);

        var investor =
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-001");

        Assert.Equal(100m, investor.ConfirmedCapital);
        Assert.Equal(2, result.Allocations.Count);
    }

    [Fact]
    public void Calculate_ShouldOrderAllocationsByRoleAndCode()
    {
        var result =
            Calculate(
                revenue: 300,
                cost: 200,
                investorCapital: 150,
                partnerCapital: 50,
                managingPartnerCode: "MITRA-Z",
                managingPartnerName: "Mitra Z",
                contributors:
                [
                    Contributor(
                        "INV-B",
                        "Investor B",
                        CapitalContributorRole.Investor,
                        50),
                    Contributor(
                        "MITRA-A",
                        "Mitra A",
                        CapitalContributorRole.Partner,
                        50),
                    Contributor(
                        "INV-A",
                        "Investor A",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        Assert.Collection(
            result.Allocations,
            first =>
            {
                Assert.Equal("INV-A", first.ContributorCodeSnapshot);
                Assert.Equal(1, first.Sequence);
            },
            second =>
            {
                Assert.Equal("INV-B", second.ContributorCodeSnapshot);
                Assert.Equal(2, second.Sequence);
            },
            third =>
            {
                Assert.Equal("MITRA-A", third.ContributorCodeSnapshot);
                Assert.Equal(3, third.Sequence);
            },
            fourth =>
            {
                Assert.Equal("MITRA-Z", fourth.ContributorCodeSnapshot);
                Assert.Equal(4, fourth.Sequence);
            });
    }

    [Fact]
    public void Calculate_CapitalProfitRemainder_ShouldUseStableLastContributor()
    {
        var result =
            Calculate(
                revenue: 100.02m,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-A",
                        "Investor A",
                        CapitalContributorRole.Investor,
                        33.33m),
                    Contributor(
                        "INV-B",
                        "Investor B",
                        CapitalContributorRole.Investor,
                        33.33m),
                    Contributor(
                        "INV-C",
                        "Investor C",
                        CapitalContributorRole.Investor,
                        33.34m)
                ]);

        Assert.Equal(0.01m, result.ManagementProfitPool);
        Assert.Equal(0.01m, result.CapitalProfitPool);

        Assert.Equal(
            0m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-A").CapitalProfitShare);

        Assert.Equal(
            0m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-B").CapitalProfitShare);

        Assert.Equal(
            0.01m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-C").CapitalProfitShare);
    }

    [Fact]
    public void Calculate_LossRecoveryRemainder_ShouldUseStableLastContributor()
    {
        var result =
            Calculate(
                revenue: 0.01m,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-A",
                        "Investor A",
                        CapitalContributorRole.Investor,
                        33.33m),
                    Contributor(
                        "INV-B",
                        "Investor B",
                        CapitalContributorRole.Investor,
                        33.33m),
                    Contributor(
                        "INV-C",
                        "Investor C",
                        CapitalContributorRole.Investor,
                        33.34m)
                ]);

        Assert.Equal(
            0m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-A").CapitalRecovery);

        Assert.Equal(
            0m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-B").CapitalRecovery);

        Assert.Equal(
            0.01m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-C").CapitalRecovery);
    }

    [Fact]
    public void Calculate_BreakEven_ShouldRecoverFullCapitalWithoutProfit()
    {
        var result =
            Calculate(
                revenue: 100,
                cost: 100,
                investorCapital: 75,
                partnerCapital: 25,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        75),
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        25)
                ]);

        Assert.Equal(
            ProfitabilityOutcome.BreakEven,
            result.Outcome);

        Assert.Equal(100m, result.TotalCapitalRecovery);
        Assert.Equal(0m, result.TotalCapitalLoss);
        Assert.Equal(0m, result.TotalInvestorProfitShare);
        Assert.Equal(0m, result.TotalPartnerProfitShare);
        Assert.Equal(100m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_Loss_ShouldAllocateRecoveryAndCapitalLoss()
    {
        var result =
            Calculate(
                revenue: 60,
                cost: 100,
                investorCapital: 75,
                partnerCapital: 25,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        75),
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        25)
                ]);

        var investor =
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-001");

        var partner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-001");

        Assert.Equal(45m, investor.CapitalRecovery);
        Assert.Equal(30m, investor.CapitalLoss);
        Assert.Equal(15m, partner.CapitalRecovery);
        Assert.Equal(10m, partner.CapitalLoss);
        Assert.Equal(60m, result.TotalCapitalRecovery);
        Assert.Equal(40m, result.TotalCapitalLoss);
    }

    [Fact]
    public void Calculate_Loss_ShouldHaveNoProfitShare()
    {
        var result =
            Calculate(
                revenue: 60,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        Assert.Equal(0m, result.ManagementProfitPool);
        Assert.Equal(0m, result.CapitalProfitPool);
        Assert.Equal(0m, result.TotalInvestorProfitShare);
        Assert.Equal(0m, result.TotalPartnerProfitShare);

        Assert.All(
            result.Allocations,
            allocation =>
                Assert.Equal(
                    0m,
                    allocation.TotalProfitShare));
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(100, 100)]
    [InlineData(60, 100)]
    public void Calculate_TotalPayout_ShouldEqualRecognizedRevenue(
        decimal revenue,
        decimal cost)
    {
        var result =
            Calculate(
                revenue,
                cost,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        Assert.Equal(
            result.RecognizedRevenue,
            result.TotalPayout);
    }

    [Fact]
    public void Calculate_ManagingPartnerWithCapital_ShouldCombineShares()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 0,
                partnerCapital: 100,
                contributors:
                [
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        100)
                ]);

        var partner =
            result.Allocations.Single();

        Assert.Equal(16.67m, partner.ManagementProfitShare);
        Assert.Equal(33.33m, partner.CapitalProfitShare);
        Assert.Equal(50m, partner.TotalProfitShare);
        Assert.Equal(150m, partner.TotalPayout);
    }

    [Fact]
    public void Calculate_OtherPartner_ShouldNotReceiveManagementShare()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 0,
                partnerCapital: 100,
                managingPartnerCode: "MITRA-MGR",
                managingPartnerName: "Mitra Pengelola",
                contributors:
                [
                    Contributor(
                        "MITRA-CAP",
                        "Mitra Pemodal",
                        CapitalContributorRole.Partner,
                        100)
                ]);

        var managingPartner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-MGR");

        var capitalPartner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-CAP");

        Assert.Equal(
            result.ManagementProfitPool,
            managingPartner.ManagementProfitShare);

        Assert.Equal(
            0m,
            capitalPartner.ManagementProfitShare);

        Assert.Equal(
            result.CapitalProfitPool,
            capitalPartner.CapitalProfitShare);
    }

    [Fact]
    public void Calculate_SameCodeDifferentRole_ShouldRemainSeparate()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                managingPartnerCode: "PERSON-001",
                managingPartnerName: "Mitra Pengelola",
                contributors:
                [
                    Contributor(
                        "PERSON-001",
                        "Investor dengan Kode Sama",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        Assert.Equal(2, result.Allocations.Count);

        Assert.Contains(
            result.Allocations,
            allocation =>
                allocation.ContributorRole ==
                    CapitalContributorRole.Investor &&
                allocation.ContributorCodeSnapshot ==
                    "PERSON-001");

        Assert.Contains(
            result.Allocations,
            allocation =>
                allocation.ContributorRole ==
                    CapitalContributorRole.Partner &&
                allocation.ContributorCodeSnapshot ==
                    "PERSON-001");
    }

    [Fact]
    public void Calculate_ShouldNormalizeCodesAndNames()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                managingPartnerCode: "  mitra-001  ",
                managingPartnerName: "  Mitra Pengelola  ",
                contributors:
                [
                    Contributor(
                        "  inv-001  ",
                        "  Investor Utama  ",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        var investor =
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-001");

        var partner =
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-001");

        Assert.Equal(
            "Investor Utama",
            investor.ContributorNameSnapshot);

        Assert.Equal(
            "Mitra Pengelola",
            partner.ContributorNameSnapshot);
    }

    [Fact]
    public void Calculate_ShouldExposeEightDecimalCapitalRatio()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 3,
                investorCapital: 1,
                partnerCapital: 2,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        1),
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        2)
                ]);

        Assert.Equal(
            0.33333333m,
            Allocation(
                result,
                CapitalContributorRole.Investor,
                "INV-001").CapitalRatio);

        Assert.Equal(
            0.66666667m,
            Allocation(
                result,
                CapitalContributorRole.Partner,
                "MITRA-001").CapitalRatio);
    }

    [Fact]
    public void Calculate_ShouldExposeCalculationVersion()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        Assert.Equal(
            "SIPACUL-PS-1",
            result.CalculationVersion);
    }

    [Fact]
    public void Calculate_ZeroCost_ShouldThrow()
    {
        var report =
            Report(
                revenue: 0,
                cost: 0,
                investorCapital: 0,
                partnerCapital: 0);

        Assert.Throws<InvalidOperationException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                Array.Empty<
                    ProfitSharingContributorInput>()));
    }

    [Fact]
    public void Calculate_FundingGap_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 90,
                partnerCapital: 0);

        Assert.Throws<InvalidOperationException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        90)
                ]));
    }

    [Fact]
    public void Calculate_FundingExcess_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 110,
                partnerCapital: 0);

        Assert.Throws<InvalidOperationException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        110)
                ]));
    }

    [Fact]
    public void Calculate_InvestorCapitalMismatch_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        90),
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        10)
                ]));
    }

    [Fact]
    public void Calculate_UnsupportedRole_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        (CapitalContributorRole)999,
                        100)
                ]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_NonPositiveContributorCapital_ShouldThrow(
        decimal capital)
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        capital)
                ]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Calculate_BlankManagingPartnerCode_ShouldThrow(
        string code)
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                code,
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Calculate_BlankManagingPartnerName_ShouldThrow(
        string name)
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                name,
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]));
    }

    [Theory]
    [InlineData("MITRA SPACE")]
    [InlineData("-MITRA")]
    [InlineData("MITRA@001")]
    public void Calculate_InvalidManagingPartnerCode_ShouldThrow(
        string code)
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                code,
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]));
    }

    [Fact]
    public void Calculate_TooLongManagingPartnerName_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                new string(
                    'M',
                    ProfitSharingCalculator
                        .MaxContributorNameLength +
                    1),
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INV SPACE")]
    [InlineData("@INV")]
    public void Calculate_InvalidContributorCode_ShouldThrow(
        string code)
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        code,
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]));
    }

    [Fact]
    public void Calculate_DuplicateIdentityWithDifferentNames_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor A",
                        CapitalContributorRole.Investor,
                        50),
                    Contributor(
                        "INV-001",
                        "Investor B",
                        CapitalContributorRole.Investor,
                        50)
                ]));
    }

    [Fact]
    public void Calculate_ManagingPartnerIdentityConflict_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 0,
                partnerCapital: 100);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Nama Berbeda",
                [
                    Contributor(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        100)
                ]));
    }

    [Fact]
    public void Calculate_NullContributors_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentNullException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                null!));
    }

    [Fact]
    public void Calculate_EmptyContributorsWithCapital_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0);

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                Array.Empty<
                    ProfitSharingContributorInput>()));
    }

    [Fact]
    public void Calculate_InconsistentOutcome_ShouldThrow()
    {
        var report =
            Report(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0)
            with
            {
                Outcome = ProfitabilityOutcome.Loss
            };

        Assert.Throws<InvalidOperationException>(() =>
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]));
    }

    [Fact]
    public void Calculate_Allocations_ShouldBeReadOnly()
    {
        var result =
            Calculate(
                revenue: 150,
                cost: 100,
                investorCapital: 100,
                partnerCapital: 0,
                contributors:
                [
                    Contributor(
                        "INV-001",
                        "Investor",
                        CapitalContributorRole.Investor,
                        100)
                ]);

        var list =
            Assert.IsAssignableFrom<
                IList<ProfitSharingAllocationCalculation>>(
                    result.Allocations);

        Assert.True(list.IsReadOnly);
    }

    private static ProfitSharingCalculationResult Calculate(
        decimal revenue,
        decimal cost,
        decimal investorCapital,
        decimal partnerCapital,
        IReadOnlyCollection<ProfitSharingContributorInput>
            contributors,
        string managingPartnerCode = "MITRA-001",
        string managingPartnerName = "Mitra Pengelola")
    {
        return ProfitSharingCalculator.Calculate(
            Report(
                revenue,
                cost,
                investorCapital,
                partnerCapital),
            managingPartnerCode,
            managingPartnerName,
            contributors);
    }

    private static CropCycleProfitabilityReport Report(
        decimal revenue,
        decimal cost,
        decimal investorCapital,
        decimal partnerCapital)
    {
        return CropCycleProfitabilityReport.Calculate(
            new CropCycleProfitabilityInput(
                OrganizationId,
                CropCycleId,
                "CC-001",
                "Musim Padi",
                CommodityId,
                "PADI",
                "Padi",
                revenue,
                revenue,
                cost,
                0,
                investorCapital,
                partnerCapital,
                0,
                new DateTime(
                    2027,
                    7,
                    1,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)));
    }

    private static ProfitSharingContributorInput Contributor(
        string code,
        string name,
        CapitalContributorRole role,
        decimal capital)
    {
        return new ProfitSharingContributorInput(
            code,
            name,
            role,
            capital);
    }

    private static ProfitSharingAllocationCalculation Allocation(
        ProfitSharingCalculationResult result,
        CapitalContributorRole role,
        string code)
    {
        return result.Allocations.Single(allocation =>
            allocation.ContributorRole == role &&
            allocation.ContributorCodeSnapshot == code);
    }
}
