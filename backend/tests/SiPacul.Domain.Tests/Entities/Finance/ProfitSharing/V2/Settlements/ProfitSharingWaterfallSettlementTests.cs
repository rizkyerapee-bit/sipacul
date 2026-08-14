using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.ProfitSharing.V2.Settlements;

public sealed class ProfitSharingWaterfallSettlementTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse("30000000-0000-0000-0000-000000000001");

    private static readonly DateTime GeneratedAt =
        new(2027, 7, 24, 8, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime FinalizedAt =
        new(2027, 7, 24, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateFinalized_ShouldCopyCompleteImmutableSnapshot()
    {
        var scenario = CreateScenario();

        var settlement = CreateSettlement(scenario);

        Assert.NotEqual(Guid.Empty, settlement.Id);
        Assert.Equal(OrganizationId, settlement.OrganizationId);
        Assert.Equal(CropCycleId, settlement.CropCycleId);
        Assert.Equal(scenario.Assignment.Id, settlement.AssignmentId);
        Assert.Equal(
            scenario.Assignment.SourceSchemeId,
            settlement.SourceSchemeId);
        Assert.Equal("SET-001", settlement.Code);
        Assert.Equal("SCHEME-001", settlement.SchemeCodeSnapshot);
        Assert.Equal("Skema Uji", settlement.SchemeNameSnapshot);
        Assert.Equal("CYCLE-001", settlement.CropCycleCodeSnapshot);
        Assert.Equal("SIPACUL-PS-2", settlement.CalculationVersion);
        Assert.Equal(150_000m, settlement.RecognizedRevenue);
        Assert.Equal(50_000m, settlement.TotalProfitShare);
        Assert.Equal(150_000m, settlement.TotalPayout);
        Assert.Equal(FinalizedAt, settlement.CreatedAt);
        Assert.Equal(FinalizedAt, settlement.FinalizedAt);
        Assert.Equal(
            ProfitSharingWaterfallSettlementStatus.Finalized,
            settlement.Status);
        Assert.True(settlement.IsActive);
        Assert.Equal(2, settlement.ParticipantAllocations.Count);
        Assert.Single(settlement.PriorityAllocations);
        Assert.Empty(settlement.ResidualShares);

        var partner = Assert.Single(
            settlement.ParticipantAllocations,
            item => item.ParticipantCodeSnapshot == "MITRA");
        Assert.Equal(16_666.67m, partner.ManagementProfitShare);
        Assert.Equal(6_666.67m, partner.ResidualProfitShare);
        Assert.Equal(43_333.34m, partner.TotalPayout);
        Assert.Equal(settlement.Id, partner.ProfitSharingWaterfallSettlementId);
        Assert.Equal(FinalizedAt, partner.CreatedAt);
    }

    [Fact]
    public void CreateFinalized_WithPassiveInvestor_ShouldSnapshotReturn()
    {
        var scenario = CreateScenario(includePassiveInvestor: true);

        var settlement = CreateSettlement(scenario);

        var investor = Assert.Single(
            settlement.ParticipantAllocations,
            item => item.ParticipantCodeSnapshot == "INVESTOR");
        Assert.Equal(
            ProfitSharingParticipantRole.PassiveInvestor,
            investor.ParticipantRole);
        Assert.Equal(20_000m, investor.ConfirmedCapital);
        Assert.Equal(2_000m, investor.ReturnOnCapitalProfitShare);
        Assert.Equal(0m, investor.ResidualProfitShare);
        Assert.Equal(22_000m, investor.TotalPayout);
        Assert.Equal(2_000m, settlement.TotalReturnOnCapitalProfitShare);
        Assert.Equal(2, settlement.PriorityAllocations.Count);
    }

    [Fact]
    public void CreateFinalized_WithFixedResidual_ShouldCopyRates()
    {
        var scenario = CreateScenario(fixedResidual: true);

        var settlement = CreateSettlement(scenario);

        Assert.Equal(
            ProfitSharingResidualMethod.FixedPercentage,
            settlement.ResidualMethod);
        Assert.Equal(2, settlement.ResidualShares.Count);

        var companyShare = Assert.Single(
            settlement.ResidualShares,
            item => item.RecipientCodeSnapshot == "PERUSAHAAN");
        Assert.Equal(3m, companyShare.RateNumerator);
        Assert.Equal(4m, companyShare.RateDenominator);
        Assert.Equal(1, companyShare.Sequence);

        var company = Assert.Single(
            settlement.ParticipantAllocations,
            item => item.ParticipantCodeSnapshot == "PERUSAHAAN");
        Assert.Equal(25_000m, company.ResidualProfitShare);
    }

    [Fact]
    public void CreateFinalized_WithLoss_ShouldSnapshotCapitalLoss()
    {
        var scenario = CreateScenario(recognizedRevenue: 60_000m);

        var settlement = CreateSettlement(scenario);

        Assert.Equal(ProfitabilityOutcome.Loss, settlement.Outcome);
        Assert.Equal(-40_000m, settlement.NetProfit);
        Assert.Equal(60_000m, settlement.TotalCapitalRecovery);
        Assert.Equal(40_000m, settlement.TotalCapitalLoss);
        Assert.Equal(0m, settlement.TotalProfitShare);
        Assert.Equal(60_000m, settlement.TotalPayout);

        var company = Assert.Single(
            settlement.ParticipantAllocations,
            item => item.ParticipantCodeSnapshot == "PERUSAHAAN");
        Assert.Equal(48_000m, company.CapitalRecovery);
        Assert.Equal(32_000m, company.CapitalLoss);
    }

    [Fact]
    public void CreateFinalized_WithDifferentOrganization_ShouldReject()
    {
        var scenario = CreateScenario();

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingWaterfallSettlement.CreateFinalized(
                Guid.NewGuid(),
                CropCycleId,
                "SET-001",
                new DateOnly(2027, 7, 24),
                scenario.Assignment,
                scenario.Profitability,
                scenario.Calculation,
                null,
                FinalizedAt));
    }

    [Fact]
    public void CreateFinalized_WithDifferentCropCycle_ShouldReject()
    {
        var scenario = CreateScenario();

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingWaterfallSettlement.CreateFinalized(
                OrganizationId,
                Guid.NewGuid(),
                "SET-001",
                new DateOnly(2027, 7, 24),
                scenario.Assignment,
                scenario.Profitability,
                scenario.Calculation,
                null,
                FinalizedAt));
    }

    [Fact]
    public void CreateFinalized_WithOutstandingReceivable_ShouldReject()
    {
        var scenario = CreateScenario();
        var report = CreateReport(
            recognizedRevenue: 150_000m,
            collectedRevenue: 140_000m,
            cultivationCost: 100_000m,
            investorCapital: 80_000m,
            partnerCapital: 20_000m);

        Assert.Throws<InvalidOperationException>(() =>
            CreateSettlement(scenario with { Profitability = report }));
    }

    [Fact]
    public void CreateFinalized_WithAvailableHarvest_ShouldReject()
    {
        var scenario = CreateScenario();
        var report = CreateReport(
            recognizedRevenue: 150_000m,
            collectedRevenue: 150_000m,
            cultivationCost: 100_000m,
            investorCapital: 80_000m,
            partnerCapital: 20_000m,
            availableHarvest: 12.5m);

        Assert.Throws<InvalidOperationException>(() =>
            CreateSettlement(scenario with { Profitability = report }));
    }

    [Fact]
    public void CreateFinalized_WithZeroCost_ShouldReject()
    {
        var scenario = CreateScenario();
        var report = CreateReport(
            recognizedRevenue: 0m,
            collectedRevenue: 0m,
            cultivationCost: 0m,
            investorCapital: 0m,
            partnerCapital: 0m);

        Assert.Throws<InvalidOperationException>(() =>
            CreateSettlement(scenario with { Profitability = report }));
    }

    [Fact]
    public void CreateFinalized_WithUnbalancedCapital_ShouldReject()
    {
        var scenario = CreateScenario();
        var report = CreateReport(
            recognizedRevenue: 150_000m,
            collectedRevenue: 150_000m,
            cultivationCost: 100_000m,
            investorCapital: 60_000m,
            partnerCapital: 20_000m);

        Assert.Throws<InvalidOperationException>(() =>
            CreateSettlement(scenario with { Profitability = report }));
    }

    [Fact]
    public void CreateFinalized_WithCalculationReportMismatch_ShouldReject()
    {
        var scenario = CreateScenario();
        var calculation = scenario.Calculation with
        {
            RecognizedRevenue = 149_999m
        };

        Assert.Throws<InvalidOperationException>(() =>
            CreateSettlement(scenario with { Calculation = calculation }));
    }

    [Fact]
    public void CreateFinalized_WithAssignmentMismatch_ShouldReject()
    {
        var scenario = CreateScenario();
        var allocations = scenario.Calculation.Allocations
            .Select(item =>
                item.ParticipantCodeSnapshot == "MITRA"
                    ? item with
                    {
                        ParticipantNameSnapshot = "Mitra Berbeda"
                    }
                    : item)
            .ToArray();
        var calculation = scenario.Calculation with
        {
            Allocations = allocations
        };

        Assert.Throws<InvalidOperationException>(() =>
            CreateSettlement(scenario with { Calculation = calculation }));
    }

    [Fact]
    public void CreateFinalized_WithNonUtcTime_ShouldReject()
    {
        var scenario = CreateScenario();
        var localTime = new DateTime(
            2027,
            7,
            24,
            9,
            0,
            0,
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            CreateSettlement(scenario, localTime));
    }

    [Fact]
    public void Void_ShouldPreserveSnapshotAndMarkInactive()
    {
        var settlement = CreateSettlement(CreateScenario());
        var payout = settlement.TotalPayout;
        var allocationIds = settlement.ParticipantAllocations
            .Select(item => item.Id)
            .ToArray();
        var voidedAt = FinalizedAt.AddHours(1);

        settlement.Void("  Data sumber dibatalkan  ", voidedAt);

        Assert.Equal(
            ProfitSharingWaterfallSettlementStatus.Voided,
            settlement.Status);
        Assert.False(settlement.IsActive);
        Assert.Equal(voidedAt, settlement.VoidedAt);
        Assert.Equal(voidedAt, settlement.UpdatedAt);
        Assert.Equal("Data sumber dibatalkan", settlement.VoidReason);
        Assert.Equal(payout, settlement.TotalPayout);
        Assert.Equal(
            allocationIds,
            settlement.ParticipantAllocations
                .Select(item => item.Id)
                .ToArray());
    }

    [Fact]
    public void Void_WithBlankReason_ShouldRejectWithoutChangingStatus()
    {
        var settlement = CreateSettlement(CreateScenario());

        Assert.Throws<ArgumentException>(() =>
            settlement.Void("  ", FinalizedAt.AddHours(1)));

        Assert.Equal(
            ProfitSharingWaterfallSettlementStatus.Finalized,
            settlement.Status);
        Assert.Null(settlement.VoidedAt);
    }

    [Fact]
    public void Void_WhenAlreadyVoided_ShouldReject()
    {
        var settlement = CreateSettlement(CreateScenario());
        settlement.Void("Koreksi", FinalizedAt.AddHours(1));

        Assert.Throws<InvalidOperationException>(() =>
            settlement.Void("Koreksi kedua", FinalizedAt.AddHours(2)));
    }

    private static ProfitSharingWaterfallSettlement CreateSettlement(
        Scenario scenario,
        DateTime? finalizedAt = null)
    {
        return ProfitSharingWaterfallSettlement.CreateFinalized(
            OrganizationId,
            CropCycleId,
            " set-001 ",
            new DateOnly(2027, 7, 24),
            scenario.Assignment,
            scenario.Profitability,
            scenario.Calculation,
            "Snapshot final uji",
            finalizedAt ?? FinalizedAt);
    }

    private static Scenario CreateScenario(
        bool includePassiveInvestor = false,
        bool fixedResidual = false,
        decimal recognizedRevenue = 150_000m)
    {
        var participants = new List<
            ProfitSharingSchemeParticipantDefinition>
        {
            new(
                "PERUSAHAAN",
                "Perusahaan",
                ProfitSharingParticipantRole.Company,
                true,
                1),
            new(
                "MITRA",
                "Mitra Tani",
                ProfitSharingParticipantRole.ManagingPartner,
                true,
                2)
        };

        var rules = new List<
            ProfitSharingSchemePriorityRuleDefinition>
        {
            new(
                "BIAYA-KELOLA",
                ProfitSharingPriorityRuleType.ManagementShare,
                "MITRA",
                ProfitSharingRate.FromFraction(1m, 3m),
                1)
        };

        if (includePassiveInvestor)
        {
            participants.Add(
                new ProfitSharingSchemeParticipantDefinition(
                    "INVESTOR",
                    "Investor Pasif",
                    ProfitSharingParticipantRole.PassiveInvestor,
                    false,
                    3));
            rules.Add(
                new ProfitSharingSchemePriorityRuleDefinition(
                    "IMBAL-INVESTOR",
                    ProfitSharingPriorityRuleType.ReturnOnCapital,
                    "INVESTOR",
                    ProfitSharingRate.FromPercentage(10m),
                    2));
        }

        var residualMethod = fixedResidual
            ? ProfitSharingResidualMethod.FixedPercentage
            : ProfitSharingResidualMethod.ProRataCapital;

        IReadOnlyCollection<ProfitSharingSchemeResidualShareDefinition>
            residualShares = fixedResidual
                ?
                [
                    new ProfitSharingSchemeResidualShareDefinition(
                        "PERUSAHAAN",
                        ProfitSharingRate.FromFraction(3m, 4m),
                        1),
                    new ProfitSharingSchemeResidualShareDefinition(
                        "MITRA",
                        ProfitSharingRate.FromFraction(1m, 4m),
                        2)
                ]
                : [];

        var scheme = ProfitSharingScheme.CreateDraft(
            OrganizationId,
            "SCHEME-001",
            "Skema Uji",
            "Skema snapshot final",
            participants,
            rules,
            residualMethod,
            null,
            residualShares);
        scheme.Activate();

        var assignment = ProfitSharingSchemeAssignment.Create(
            OrganizationId,
            CropCycleId,
            scheme);

        var report = CreateReport(
            recognizedRevenue,
            recognizedRevenue,
            100_000m,
            80_000m,
            20_000m);

        var companyCapital = includePassiveInvestor
            ? 60_000m
            : 80_000m;

        var capitalByCode = new Dictionary<string, decimal>(
            StringComparer.Ordinal)
        {
            ["PERUSAHAAN"] = companyCapital,
            ["MITRA"] = 20_000m
        };

        if (includePassiveInvestor)
        {
            capitalByCode["INVESTOR"] = 20_000m;
        }

        var calculation = ProfitSharingWaterfallCalculator.Calculate(
            report,
            BuildWaterfallInput(assignment, capitalByCode));

        return new Scenario(assignment, report, calculation);
    }

    private static CropCycleProfitabilityReport CreateReport(
        decimal recognizedRevenue,
        decimal collectedRevenue,
        decimal cultivationCost,
        decimal investorCapital,
        decimal partnerCapital,
        decimal availableHarvest = 0m)
    {
        return CropCycleProfitabilityReport.Calculate(
            new CropCycleProfitabilityInput(
                OrganizationId,
                CropCycleId,
                "CYCLE-001",
                "Siklus Uji",
                CommodityId,
                "CABAI",
                "Cabai",
                recognizedRevenue,
                collectedRevenue,
                cultivationCost,
                0m,
                investorCapital,
                partnerCapital,
                availableHarvest,
                GeneratedAt));
    }

    private static ProfitSharingWaterfallSchemeInput BuildWaterfallInput(
        ProfitSharingSchemeAssignment assignment,
        IReadOnlyDictionary<string, decimal> capitalByCode)
    {
        var participants = assignment.Participants
            .OrderBy(item => item.Sequence)
            .Select(item =>
                new ProfitSharingWaterfallParticipantInput(
                    item.ParticipantCode,
                    item.ParticipantName,
                    item.ParticipantRole,
                    capitalByCode[item.ParticipantCode],
                    item.ParticipatesInResidualProfit,
                    item.Sequence))
            .ToArray();

        var rules = assignment.PriorityRules
            .OrderBy(item => item.Sequence)
            .Select(item =>
                new ProfitSharingPriorityRuleInput(
                    item.RuleCode,
                    item.RuleType,
                    item.RecipientCode,
                    ProfitSharingRate.FromFraction(
                        item.RateNumerator,
                        item.RateDenominator),
                    item.Sequence))
            .ToArray();

        var residualPolicy = assignment.ResidualMethod switch
        {
            ProfitSharingResidualMethod.ProRataCapital =>
                ProfitSharingResidualPolicyInput.ProRataCapital(),
            ProfitSharingResidualMethod.FixedPercentage =>
                ProfitSharingResidualPolicyInput.FixedPercentage(
                    assignment.ResidualShares
                        .OrderBy(item => item.Sequence)
                        .Select(item =>
                            new ProfitSharingResidualShareInput(
                                item.RecipientCode,
                                ProfitSharingRate.FromFraction(
                                    item.RateNumerator,
                                    item.RateDenominator),
                                item.Sequence))
                        .ToArray()),
            _ => ProfitSharingResidualPolicyInput.RemainderToParticipant(
                assignment.ResidualRecipientCode ??
                throw new InvalidOperationException(
                    "Residual recipient is missing."))
        };

        return new ProfitSharingWaterfallSchemeInput(
            participants,
            rules,
            residualPolicy);
    }

    private sealed record Scenario(
        ProfitSharingSchemeAssignment Assignment,
        CropCycleProfitabilityReport Profitability,
        ProfitSharingWaterfallCalculationResult Calculation);
}
