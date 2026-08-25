using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.ProfitSharing.V2;

public sealed class ProfitSharingWaterfallCalculatorTests
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
    public void Calculate_InternalCompany_ShouldReceiveAllFunds()
    {
        var result =
            Calculate(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        100_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1)
                ],
                priorityRules: [],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .RemainderToParticipant(
                            "PERUSAHAAN"));

        var company =
            Allocation(result, "PERUSAHAAN");

        Assert.Equal(50_000_000m, result.NetProfit);
        Assert.Equal(0m, result.TotalPriorityProfitShare);
        Assert.Equal(
            50_000_000m,
            result.TotalResidualProfitShare);
        Assert.Equal(100_000_000m, company.CapitalRecovery);
        Assert.Equal(50_000_000m, company.ResidualProfitShare);
        Assert.Equal(150_000_000m, company.TotalPayout);
        Assert.Equal(150_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_CompanyCapitalManagedByPartner_ShouldUseWaterfall()
    {
        var result =
            Calculate(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        100_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "MITRA",
                        "Mitra Tani",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        0m,
                        participatesInResidualProfit: false,
                        sequence: 2)
                ],
                priorityRules:
                [
                    ManagementRule(
                        "KELOLA-MITRA",
                        "MITRA",
                        ProfitSharingRate.FromFraction(1, 3),
                        sequence: 1)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .ProRataCapital());

        var company =
            Allocation(result, "PERUSAHAAN");
        var partner =
            Allocation(result, "MITRA");

        Assert.Equal(
            16_666_666.67m,
            partner.ManagementProfitShare);
        Assert.Equal(
            33_333_333.33m,
            company.ResidualProfitShare);
        Assert.Equal(
            133_333_333.33m,
            company.TotalPayout);
        Assert.Equal(
            16_666_666.67m,
            partner.TotalPayout);
        Assert.Equal(150_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_CompanyAndManagingPartnerCapital_ShouldMatchExample()
    {
        var result =
            Calculate(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        80_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "MITRA",
                        "Mitra Tani",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        20_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 2)
                ],
                priorityRules:
                [
                    ManagementRule(
                        "KELOLA-MITRA",
                        "MITRA",
                        ProfitSharingRate.FromFraction(1, 3),
                        sequence: 1)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .ProRataCapital());

        var company =
            Allocation(result, "PERUSAHAAN");
        var partner =
            Allocation(result, "MITRA");

        Assert.Equal(
            16_666_666.67m,
            partner.ManagementProfitShare);
        Assert.Equal(
            6_666_666.67m,
            partner.ResidualProfitShare);
        Assert.Equal(
            43_333_333.34m,
            partner.TotalPayout);
        Assert.Equal(
            26_666_666.66m,
            company.ResidualProfitShare);
        Assert.Equal(
            106_666_666.66m,
            company.TotalPayout);
        Assert.Equal(150_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_CompanyAndPassiveInvestor_ShouldUseReturnOnCapital()
    {
        var result =
            Calculate(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        80_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 2)
                ],
                priorityRules:
                [
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(15),
                        sequence: 1)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .RemainderToParticipant(
                            "PERUSAHAAN"));

        var company =
            Allocation(result, "PERUSAHAAN");
        var investor =
            Allocation(result, "INVESTOR-A");

        Assert.Equal(
            3_000_000m,
            investor.ReturnOnCapitalProfitShare);
        Assert.Equal(23_000_000m, investor.TotalPayout);
        Assert.Equal(
            47_000_000m,
            company.ResidualProfitShare);
        Assert.Equal(127_000_000m, company.TotalPayout);
        Assert.Equal(150_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_CompanyInvestorAndPartner_ShouldUseAllStages()
    {
        var result =
            Calculate(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        60_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 2),
                    Participant(
                        "MITRA",
                        "Mitra Tani",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        20_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 3)
                ],
                priorityRules:
                [
                    ManagementRule(
                        "KELOLA-MITRA",
                        "MITRA",
                        ProfitSharingRate.FromFraction(1, 3),
                        sequence: 1),
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(15),
                        sequence: 2)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .ProRataCapital());

        var company =
            Allocation(result, "PERUSAHAAN");
        var investor =
            Allocation(result, "INVESTOR-A");
        var partner =
            Allocation(result, "MITRA");

        Assert.Equal(
            16_666_666.67m,
            result.TotalManagementProfitShare);
        Assert.Equal(
            3_000_000m,
            result.TotalReturnOnCapitalProfitShare);
        Assert.Equal(
            30_333_333.33m,
            result.TotalResidualProfitShare);
        Assert.Equal(82_750_000m, company.TotalPayout);
        Assert.Equal(23_000_000m, investor.TotalPayout);
        Assert.Equal(44_250_000m, partner.TotalPayout);
        Assert.Equal(150_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_MultiplePassiveInvestors_ShouldUseOwnCapitalRates()
    {
        var result =
            Calculate(
                revenue: 120_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        50_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        30_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 2),
                    Participant(
                        "INVESTOR-B",
                        "Investor Pasif B",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 3)
                ],
                priorityRules:
                [
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(10),
                        sequence: 1),
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-B",
                        "INVESTOR-B",
                        ProfitSharingRate.FromPercentage(20),
                        sequence: 2)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .RemainderToParticipant(
                            "PERUSAHAAN"));

        Assert.Equal(
            3_000_000m,
            Allocation(result, "INVESTOR-A")
                .ReturnOnCapitalProfitShare);
        Assert.Equal(
            4_000_000m,
            Allocation(result, "INVESTOR-B")
                .ReturnOnCapitalProfitShare);
        Assert.Equal(
            13_000_000m,
            Allocation(result, "PERUSAHAAN")
                .ResidualProfitShare);
        Assert.Equal(120_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_HybridInvestor_ShouldReceiveReturnAndResidual()
    {
        var result =
            Calculate(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        80_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 2)
                ],
                priorityRules:
                [
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(10),
                        sequence: 1)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .ProRataCapital());

        var investor =
            Allocation(result, "INVESTOR-A");

        Assert.Equal(
            2_000_000m,
            investor.ReturnOnCapitalProfitShare);
        Assert.Equal(
            9_600_000m,
            investor.ResidualProfitShare);
        Assert.Equal(11_600_000m, investor.TotalProfitShare);
        Assert.Equal(31_600_000m, investor.TotalPayout);
        Assert.Equal(150_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_InsufficientProfit_ShouldRespectPriorityAndCap()
    {
        var result =
            Calculate(
                revenue: 102_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        80_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 2),
                    Participant(
                        "MITRA",
                        "Mitra Tani",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        0m,
                        participatesInResidualProfit: false,
                        sequence: 3)
                ],
                priorityRules:
                [
                    ManagementRule(
                        "KELOLA-MITRA",
                        "MITRA",
                        ProfitSharingRate.FromPercentage(75),
                        sequence: 1),
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(15),
                        sequence: 2)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .RemainderToParticipant(
                            "PERUSAHAAN"));

        var investorRule =
            Assert.Single(
                result.PriorityAllocations,
                allocation =>
                    allocation.RuleCode ==
                        "IMBAL-INVESTOR-A");

        Assert.Equal(3_000_000m, investorRule.RequestedAmount);
        Assert.Equal(500_000m, investorRule.AllocatedAmount);
        Assert.Equal(2_500_000m, investorRule.UnallocatedAmount);
        Assert.Equal(0m, result.TotalResidualProfitShare);
        Assert.Equal(2_000_000m, result.TotalProfitShare);
        Assert.Equal(102_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_ReorderedRules_ShouldChangeInsufficientProfitPriority()
    {
        var result =
            Calculate(
                revenue: 102_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        80_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 2),
                    Participant(
                        "MITRA",
                        "Mitra Tani",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        0m,
                        participatesInResidualProfit: false,
                        sequence: 3)
                ],
                priorityRules:
                [
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(15),
                        sequence: 1),
                    ManagementRule(
                        "KELOLA-MITRA",
                        "MITRA",
                        ProfitSharingRate.FromPercentage(75),
                        sequence: 2)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .RemainderToParticipant(
                            "PERUSAHAAN"));

        var investorRule =
            result.PriorityAllocations[0];
        var managementRule =
            result.PriorityAllocations[1];

        Assert.Equal(2_000_000m, investorRule.AllocatedAmount);
        Assert.Equal(1_000_000m, investorRule.UnallocatedAmount);
        Assert.Equal(0m, managementRule.AllocatedAmount);
        Assert.Equal(1_500_000m, managementRule.UnallocatedAmount);
        Assert.Equal(102_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_Loss_ShouldSkipProfitAndRecoverCapitalProRata()
    {
        var result =
            Calculate(
                revenue: 70_000_000m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        60_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 1),
                    Participant(
                        "INVESTOR-A",
                        "Investor Pasif A",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        20_000_000m,
                        participatesInResidualProfit: false,
                        sequence: 2),
                    Participant(
                        "MITRA",
                        "Mitra Tani",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        20_000_000m,
                        participatesInResidualProfit: true,
                        sequence: 3)
                ],
                priorityRules:
                [
                    ManagementRule(
                        "KELOLA-MITRA",
                        "MITRA",
                        ProfitSharingRate.FromFraction(1, 3),
                        sequence: 1),
                    ReturnOnCapitalRule(
                        "IMBAL-INVESTOR-A",
                        "INVESTOR-A",
                        ProfitSharingRate.FromPercentage(15),
                        sequence: 2)
                ],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .ProRataCapital());

        var company =
            Allocation(result, "PERUSAHAAN");
        var investor =
            Allocation(result, "INVESTOR-A");
        var partner =
            Allocation(result, "MITRA");

        Assert.Equal(ProfitabilityOutcome.Loss, result.Outcome);
        Assert.Equal(0m, result.TotalProfitShare);
        Assert.Equal(42_000_000m, company.CapitalRecovery);
        Assert.Equal(14_000_000m, investor.CapitalRecovery);
        Assert.Equal(14_000_000m, partner.CapitalRecovery);
        Assert.Equal(30_000_000m, result.TotalCapitalLoss);
        Assert.All(
            result.PriorityAllocations,
            allocation =>
            {
                Assert.Equal(0m, allocation.BaseAmount);
                Assert.Equal(0m, allocation.AllocatedAmount);
                Assert.Equal(0m, allocation.UnallocatedAmount);
            });
        Assert.Equal(70_000_000m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_FixedResidual_ShouldKeepRoundingRemainder()
    {
        var result =
            Calculate(
                revenue: 100_000_000.01m,
                cost: 100_000_000m,
                participants:
                [
                    Participant(
                        "A",
                        "Pihak A",
                        ProfitSharingParticipantRole.Company,
                        33_333_333.33m,
                        participatesInResidualProfit: false,
                        sequence: 1),
                    Participant(
                        "B",
                        "Pihak B",
                        ProfitSharingParticipantRole
                            .PassiveInvestor,
                        33_333_333.33m,
                        participatesInResidualProfit: false,
                        sequence: 2),
                    Participant(
                        "C",
                        "Pihak C",
                        ProfitSharingParticipantRole
                            .ManagingPartner,
                        33_333_333.34m,
                        participatesInResidualProfit: false,
                        sequence: 3)
                ],
                priorityRules: [],
                residualPolicy:
                    ProfitSharingResidualPolicyInput
                        .FixedPercentage(
                        [
                            new ProfitSharingResidualShareInput(
                                "A",
                                ProfitSharingRate.FromFraction(1, 3),
                                1),
                            new ProfitSharingResidualShareInput(
                                "B",
                                ProfitSharingRate.FromFraction(1, 3),
                                2),
                            new ProfitSharingResidualShareInput(
                                "C",
                                ProfitSharingRate.FromFraction(1, 3),
                                3)
                        ]));

        Assert.Equal(0m, Allocation(result, "A").ResidualProfitShare);
        Assert.Equal(0m, Allocation(result, "B").ResidualProfitShare);
        Assert.Equal(0.01m, Allocation(result, "C").ResidualProfitShare);
        Assert.Equal(100_000_000.01m, result.TotalPayout);
    }

    [Fact]
    public void Calculate_LegacyPreset_ShouldMatchVersionOne()
    {
        var report =
            Report(
                revenue: 150_000_000m,
                cost: 100_000_000m,
                investorCapital: 80_000_000m,
                partnerCapital: 20_000_000m);

        var versionOne =
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA",
                "Mitra Tani",
                [
                    new ProfitSharingContributorInput(
                        "PERUSAHAAN",
                        "Perusahaan",
                        CapitalContributorRole.Investor,
                        80_000_000m),
                    new ProfitSharingContributorInput(
                        "MITRA",
                        "Mitra Tani",
                        CapitalContributorRole.Partner,
                        20_000_000m)
                ]);

        var versionTwo =
            ProfitSharingWaterfallCalculator.Calculate(
                report,
                new ProfitSharingWaterfallSchemeInput(
                    [
                        Participant(
                            "PERUSAHAAN",
                            "Perusahaan",
                            ProfitSharingParticipantRole.Company,
                            80_000_000m,
                            participatesInResidualProfit: true,
                            sequence: 1),
                        Participant(
                            "MITRA",
                            "Mitra Tani",
                            ProfitSharingParticipantRole
                                .ManagingPartner,
                            20_000_000m,
                            participatesInResidualProfit: true,
                            sequence: 2)
                    ],
                    [
                        ManagementRule(
                            "KELOLA-MITRA",
                            "MITRA",
                            ProfitSharingRate.FromFraction(1, 3),
                            sequence: 1)
                    ],
                    ProfitSharingResidualPolicyInput
                        .ProRataCapital()));

        var versionOneCompany =
            versionOne.Allocations.Single(allocation =>
                allocation.ContributorCodeSnapshot ==
                    "PERUSAHAAN");
        var versionOnePartner =
            versionOne.Allocations.Single(allocation =>
                allocation.ContributorCodeSnapshot == "MITRA");

        var versionTwoCompany =
            Allocation(versionTwo, "PERUSAHAAN");
        var versionTwoPartner =
            Allocation(versionTwo, "MITRA");

        Assert.Equal(
            versionOne.ManagementProfitPool,
            versionTwo.TotalManagementProfitShare);
        Assert.Equal(
            versionOne.CapitalProfitPool,
            versionTwo.TotalResidualProfitShare);
        Assert.Equal(
            versionOneCompany.TotalProfitShare,
            versionTwoCompany.TotalProfitShare);
        Assert.Equal(
            versionOneCompany.TotalPayout,
            versionTwoCompany.TotalPayout);
        Assert.Equal(
            versionOnePartner.TotalProfitShare,
            versionTwoPartner.TotalProfitShare);
        Assert.Equal(
            versionOnePartner.TotalPayout,
            versionTwoPartner.TotalPayout);
        Assert.Equal(
            ProfitSharingCalculator.CurrentCalculationVersion,
            versionOne.CalculationVersion);
        Assert.Equal(
            ProfitSharingWaterfallCalculator
                .CurrentCalculationVersion,
            versionTwo.CalculationVersion);
    }

    [Fact]
    public void Calculate_InvalidCapitalTotal_ShouldRejectScheme()
    {
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                Calculate(
                    revenue: 150_000_000m,
                    cost: 100_000_000m,
                    participants:
                    [
                        Participant(
                            "PERUSAHAAN",
                            "Perusahaan",
                            ProfitSharingParticipantRole.Company,
                            90_000_000m,
                            participatesInResidualProfit: true,
                            sequence: 1)
                    ],
                    priorityRules: [],
                    residualPolicy:
                        ProfitSharingResidualPolicyInput
                            .RemainderToParticipant(
                                "PERUSAHAAN")));

        Assert.Contains(
            "Participant capital must equal",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Calculate_InvalidFixedPercentages_ShouldRejectScheme()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                Calculate(
                    revenue: 150_000_000m,
                    cost: 100_000_000m,
                    participants:
                    [
                        Participant(
                            "PERUSAHAAN",
                            "Perusahaan",
                            ProfitSharingParticipantRole.Company,
                            100_000_000m,
                            participatesInResidualProfit: false,
                            sequence: 1)
                    ],
                    priorityRules: [],
                    residualPolicy:
                        ProfitSharingResidualPolicyInput
                            .FixedPercentage(
                            [
                                new ProfitSharingResidualShareInput(
                                    "PERUSAHAAN",
                                    ProfitSharingRate
                                        .FromPercentage(90),
                                    1)
                            ])));

        Assert.Contains(
            "must total 100%",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Rate_FractionAndPercentage_ShouldPreserveIntent()
    {
        var oneThird =
            ProfitSharingRate.FromFraction(1, 3);
        var fifteenPercent =
            ProfitSharingRate.FromPercentage(15);

        Assert.Equal(1m, oneThird.Numerator);
        Assert.Equal(3m, oneThird.Denominator);
        Assert.Equal(15m, fifteenPercent.Numerator);
        Assert.Equal(100m, fifteenPercent.Denominator);
    }

    private static ProfitSharingWaterfallCalculationResult
        Calculate(
            decimal revenue,
            decimal cost,
            IReadOnlyCollection<
                ProfitSharingWaterfallParticipantInput>
                participants,
            IReadOnlyCollection<
                ProfitSharingPriorityRuleInput> priorityRules,
            ProfitSharingResidualPolicyInput residualPolicy)
    {
        return ProfitSharingWaterfallCalculator.Calculate(
            Report(
                revenue,
                cost,
                investorCapital: cost,
                partnerCapital: 0m),
            new ProfitSharingWaterfallSchemeInput(
                participants,
                priorityRules,
                residualPolicy));
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
                0m,
                investorCapital,
                partnerCapital,
                0m,
                new DateTime(
                    2027,
                    7,
                    1,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)));
    }

    private static ProfitSharingWaterfallParticipantInput
        Participant(
            string code,
            string name,
            ProfitSharingParticipantRole role,
            decimal capital,
            bool participatesInResidualProfit,
            int sequence)
    {
        return new ProfitSharingWaterfallParticipantInput(
            code,
            name,
            role,
            capital,
            participatesInResidualProfit,
            sequence);
    }

    private static ProfitSharingPriorityRuleInput
        ManagementRule(
            string code,
            string recipientCode,
            ProfitSharingRate rate,
            int sequence)
    {
        return new ProfitSharingPriorityRuleInput(
            code,
            ProfitSharingPriorityRuleType.ManagementShare,
            recipientCode,
            rate,
            sequence);
    }

    private static ProfitSharingPriorityRuleInput
        ReturnOnCapitalRule(
            string code,
            string recipientCode,
            ProfitSharingRate rate,
            int sequence)
    {
        return new ProfitSharingPriorityRuleInput(
            code,
            ProfitSharingPriorityRuleType.ReturnOnCapital,
            recipientCode,
            rate,
            sequence);
    }

    private static ProfitSharingWaterfallAllocationCalculation
        Allocation(
            ProfitSharingWaterfallCalculationResult result,
            string participantCode)
    {
        return result.Allocations.Single(allocation =>
            allocation.ParticipantCodeSnapshot ==
                participantCode);
    }
}
