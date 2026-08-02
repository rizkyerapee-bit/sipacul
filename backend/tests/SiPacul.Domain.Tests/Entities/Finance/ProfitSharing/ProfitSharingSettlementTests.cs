using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance.ProfitSharing;

public sealed class ProfitSharingSettlementTests
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

    private static readonly DateOnly SettlementDate =
        new(2027, 7, 10);

    [Fact]
    public void CreateDraft_WithProfit_ShouldSnapshotCalculation()
    {
        var settlement = CreateSettlement();

        Assert.NotEqual(Guid.Empty, settlement.Id);
        Assert.Equal(OrganizationId, settlement.OrganizationId);
        Assert.Equal(CropCycleId, settlement.CropCycleId);
        Assert.Equal("SET-001", settlement.Code);
        Assert.Equal(SettlementDate, settlement.SettlementDate);
        Assert.Equal(
            "MITRA-001",
            settlement.ManagingPartnerCode);

        Assert.Equal(
            "Mitra Pengelola",
            settlement.ManagingPartnerName);

        Assert.Equal(600m, settlement.RecognizedRevenue);
        Assert.Equal(600m, settlement.CollectedRevenue);
        Assert.Equal(0m, settlement.OutstandingReceivable);
        Assert.Equal(200m, settlement.ActivityResourceCost);
        Assert.Equal(100m, settlement.ManualExpenseCost);
        Assert.Equal(300m, settlement.TotalCultivationCost);
        Assert.Equal(300m, settlement.NetProfit);

        Assert.Equal(
            ProfitabilityOutcome.Profit,
            settlement.Outcome);

        Assert.Equal(100m, settlement.ManagementProfitPool);
        Assert.Equal(200m, settlement.CapitalProfitPool);
        Assert.Equal(200m, settlement.TotalInvestorCapital);
        Assert.Equal(100m, settlement.TotalPartnerCapital);
        Assert.Equal(300m, settlement.TotalCapital);
        Assert.Equal(300m, settlement.TotalCapitalRecovery);
        Assert.Equal(0m, settlement.TotalCapitalLoss);
        Assert.Equal(133.33m, settlement.TotalInvestorProfitShare);
        Assert.Equal(166.67m, settlement.TotalPartnerProfitShare);
        Assert.Equal(600m, settlement.TotalPayout);

        Assert.Equal(
            ProfitSharingCalculator.CurrentCalculationVersion,
            settlement.CalculationVersion);

        Assert.Equal(
            ProfitSharingSettlementStatus.Draft,
            settlement.Status);

        Assert.False(settlement.IsActive);
        Assert.Null(settlement.FinalizedAt);
        Assert.Null(settlement.VoidedAt);
        Assert.Null(settlement.VoidReason);
        Assert.Equal("Catatan settlement", settlement.Notes);
        Assert.Equal(2, settlement.Allocations.Count);
    }

    [Fact]
    public void CreateDraft_ShouldNormalizeIdentityAndNotes()
    {
        var settlement =
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "  set.abc_01-x  ",
                SettlementDate,
                "  mitra-001  ",
                "  Mitra Pengelola  ",
                CreateCalculation(),
                "  Catatan settlement  ");

        Assert.Equal("SET.ABC_01-X", settlement.Code);

        Assert.Equal(
            "MITRA-001",
            settlement.ManagingPartnerCode);

        Assert.Equal(
            "Mitra Pengelola",
            settlement.ManagingPartnerName);

        Assert.Equal("Catatan settlement", settlement.Notes);
    }

    [Fact]
    public void CreateDraft_WithBlankNotes_ShouldUseNull()
    {
        var settlement =
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-NULL",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                " ");

        Assert.Null(settlement.Notes);
    }

    [Fact]
    public void CreateDraft_ShouldCopyAllocationSnapshots()
    {
        var settlement = CreateSettlement();

        var investor =
            Allocation(
                settlement,
                CapitalContributorRole.Investor,
                "INV-001");

        Assert.NotEqual(Guid.Empty, investor.Id);
        Assert.Equal(
            settlement.OrganizationId,
            investor.OrganizationId);

        Assert.Equal(
            settlement.Id,
            investor.ProfitSharingSettlementId);

        Assert.Equal(
            "Investor Utama",
            investor.ContributorNameSnapshot);

        Assert.Equal(200m, investor.ConfirmedCapital);
        Assert.Equal(0.66666667m, investor.CapitalRatio);
        Assert.Equal(200m, investor.CapitalRecovery);
        Assert.Equal(0m, investor.CapitalLoss);
        Assert.Equal(0m, investor.ManagementProfitShare);
        Assert.Equal(133.33m, investor.CapitalProfitShare);
        Assert.Equal(133.33m, investor.TotalProfitShare);
        Assert.Equal(333.33m, investor.TotalPayout);
        Assert.Equal(1, investor.Sequence);
        Assert.NotEqual(default, investor.CreatedAt);
    }

    [Fact]
    public void CreateDraft_ManagingPartnerWithoutCapital_ShouldStillAllocate()
    {
        var calculation =
            CreateCalculation(
                revenue: 450,
                collectedRevenue: 450,
                activityCost: 200,
                manualCost: 100,
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

        var settlement =
            CreateSettlement(calculation);

        var partner =
            Allocation(
                settlement,
                CapitalContributorRole.Partner,
                "MITRA-001");

        Assert.Equal(0m, partner.ConfirmedCapital);
        Assert.Equal(0m, partner.CapitalRatio);
        Assert.Equal(0m, partner.CapitalRecovery);
        Assert.Equal(0m, partner.CapitalLoss);
        Assert.Equal(50m, partner.ManagementProfitShare);
        Assert.Equal(0m, partner.CapitalProfitShare);
        Assert.Equal(50m, partner.TotalPayout);
    }

    [Fact]
    public void CreateDraft_WithBreakEven_ShouldSnapshotRecovery()
    {
        var calculation =
            CreateCalculation(
                revenue: 300,
                collectedRevenue: 300);

        var settlement =
            CreateSettlement(calculation);

        Assert.Equal(
            ProfitabilityOutcome.BreakEven,
            settlement.Outcome);

        Assert.Equal(0m, settlement.NetProfit);
        Assert.Equal(0m, settlement.ManagementProfitPool);
        Assert.Equal(0m, settlement.CapitalProfitPool);
        Assert.Equal(300m, settlement.TotalCapitalRecovery);
        Assert.Equal(0m, settlement.TotalCapitalLoss);
        Assert.Equal(0m, settlement.TotalInvestorProfitShare);
        Assert.Equal(0m, settlement.TotalPartnerProfitShare);
        Assert.Equal(300m, settlement.TotalPayout);
    }

    [Fact]
    public void CreateDraft_WithLoss_ShouldSnapshotCapitalLoss()
    {
        var calculation =
            CreateCalculation(
                revenue: 180,
                collectedRevenue: 180);

        var settlement =
            CreateSettlement(calculation);

        Assert.Equal(
            ProfitabilityOutcome.Loss,
            settlement.Outcome);

        Assert.Equal(-120m, settlement.NetProfit);
        Assert.Equal(0m, settlement.ManagementProfitPool);
        Assert.Equal(0m, settlement.CapitalProfitPool);
        Assert.Equal(180m, settlement.TotalCapitalRecovery);
        Assert.Equal(120m, settlement.TotalCapitalLoss);
        Assert.Equal(0m, settlement.TotalInvestorProfitShare);
        Assert.Equal(0m, settlement.TotalPartnerProfitShare);
        Assert.Equal(180m, settlement.TotalPayout);
    }

    [Fact]
    public void UpdateDraft_ShouldChangeDateAndNotes()
    {
        var settlement = CreateSettlement();

        var newDate = new DateOnly(2027, 7, 12);

        settlement.UpdateDraft(
            newDate,
            "  Catatan diperbarui  ");

        Assert.Equal(newDate, settlement.SettlementDate);
        Assert.Equal("Catatan diperbarui", settlement.Notes);
        Assert.NotNull(settlement.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_WithSameValues_ShouldNotSetUpdatedAt()
    {
        var settlement = CreateSettlement();

        settlement.UpdateDraft(
            SettlementDate,
            "Catatan settlement");

        Assert.Null(settlement.UpdatedAt);
    }

    [Fact]
    public void FinalizeSettlement_WhenFullyCollected_ShouldFinalize()
    {
        var settlement = CreateSettlement();

        settlement.FinalizeSettlement();

        Assert.Equal(
            ProfitSharingSettlementStatus.Finalized,
            settlement.Status);

        Assert.True(settlement.IsActive);
        Assert.NotNull(settlement.FinalizedAt);
        Assert.NotNull(settlement.UpdatedAt);
        Assert.Null(settlement.VoidedAt);
        Assert.Null(settlement.VoidReason);
    }

    [Fact]
    public void FinalizeSettlement_WithOutstandingReceivable_ShouldThrow()
    {
        var calculation =
            CreateCalculation(
                revenue: 600,
                collectedRevenue: 500);

        var settlement =
            CreateSettlement(calculation);

        var exception =
            Assert.Throws<InvalidOperationException>(
                settlement.FinalizeSettlement);

        Assert.Contains(
            "uncollected",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            ProfitSharingSettlementStatus.Draft,
            settlement.Status);

        Assert.Null(settlement.FinalizedAt);
    }

    [Fact]
    public void FinalizeSettlement_Twice_ShouldThrow()
    {
        var settlement = CreateSettlement();

        settlement.FinalizeSettlement();

        Assert.Throws<InvalidOperationException>(
            settlement.FinalizeSettlement);
    }

    [Fact]
    public void UpdateDraft_AfterFinalized_ShouldThrow()
    {
        var settlement = CreateSettlement();

        settlement.FinalizeSettlement();

        Assert.Throws<InvalidOperationException>(() =>
            settlement.UpdateDraft(
                SettlementDate.AddDays(1),
                "Tidak boleh"));
    }

    [Fact]
    public void Void_Draft_ShouldVoidAndPreserveSnapshot()
    {
        var settlement = CreateSettlement();

        var payout = settlement.TotalPayout;
        var allocationCount = settlement.Allocations.Count;

        settlement.Void("  Perlu koreksi sumber  ");

        Assert.Equal(
            ProfitSharingSettlementStatus.Voided,
            settlement.Status);

        Assert.False(settlement.IsActive);
        Assert.Equal(
            "Perlu koreksi sumber",
            settlement.VoidReason);

        Assert.NotNull(settlement.VoidedAt);
        Assert.Null(settlement.FinalizedAt);
        Assert.Equal(payout, settlement.TotalPayout);
        Assert.Equal(
            allocationCount,
            settlement.Allocations.Count);
    }

    [Fact]
    public void Void_Finalized_ShouldPreserveFinalizedAt()
    {
        var settlement = CreateSettlement();

        settlement.FinalizeSettlement();

        var finalizedAt = settlement.FinalizedAt;

        settlement.Void("Settlement diganti");

        Assert.Equal(
            ProfitSharingSettlementStatus.Voided,
            settlement.Status);

        Assert.Equal(finalizedAt, settlement.FinalizedAt);
        Assert.NotNull(settlement.VoidedAt);
        Assert.False(settlement.IsActive);
    }

    [Fact]
    public void Void_Twice_ShouldThrow()
    {
        var settlement = CreateSettlement();

        settlement.Void("Koreksi");

        Assert.Throws<InvalidOperationException>(() =>
            settlement.Void("Koreksi kedua"));
    }

    [Fact]
    public void FinalizeSettlement_AfterVoided_ShouldThrow()
    {
        var settlement = CreateSettlement();

        settlement.Void("Koreksi");

        Assert.Throws<InvalidOperationException>(
            settlement.FinalizeSettlement);
    }

    [Fact]
    public void UpdateDraft_AfterVoided_ShouldThrow()
    {
        var settlement = CreateSettlement();

        settlement.Void("Koreksi");

        Assert.Throws<InvalidOperationException>(() =>
            settlement.UpdateDraft(
                SettlementDate.AddDays(1),
                null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Void_WithBlankReason_ShouldThrow(
        string reason)
    {
        var settlement = CreateSettlement();

        Assert.Throws<ArgumentException>(() =>
            settlement.Void(reason));

        Assert.Equal(
            ProfitSharingSettlementStatus.Draft,
            settlement.Status);
    }

    [Fact]
    public void Void_WithTooLongReason_ShouldThrow()
    {
        var settlement = CreateSettlement();

        Assert.Throws<ArgumentException>(() =>
            settlement.Void(
                new string(
                    'V',
                    ProfitSharingSettlement
                        .MaxVoidReasonLength +
                    1)));
    }

    [Fact]
    public void MatchesCalculation_WithSameCalculation_ShouldReturnTrue()
    {
        var calculation = CreateCalculation();

        var settlement =
            CreateSettlement(calculation);

        Assert.True(
            settlement.MatchesCalculation(
                calculation));
    }

    [Fact]
    public void MatchesCalculation_WithChangedRevenue_ShouldReturnFalse()
    {
        var settlement = CreateSettlement();

        var changedCalculation =
            CreateCalculation(
                revenue: 601,
                collectedRevenue: 601);

        Assert.False(
            settlement.MatchesCalculation(
                changedCalculation));
    }

    [Fact]
    public void MatchesCalculation_WithChangedAllocation_ShouldReturnFalse()
    {
        var calculation = CreateCalculation();

        var settlement =
            CreateSettlement(calculation);

        var changedAllocations =
            calculation.Allocations
                .Select(allocation =>
                    allocation.Sequence == 1
                        ? allocation with
                        {
                            ContributorNameSnapshot =
                                "Nama Berubah"
                        }
                        : allocation)
                .ToArray();

        var changedCalculation =
            calculation with
            {
                Allocations = changedAllocations
            };

        Assert.False(
            settlement.MatchesCalculation(
                changedCalculation));
    }

    [Fact]
    public void Allocations_ShouldBeReadOnly()
    {
        var settlement = CreateSettlement();

        var list =
            Assert.IsAssignableFrom<
                IList<ProfitSharingAllocation>>(
                    settlement.Allocations);

        Assert.True(list.IsReadOnly);
    }

    [Fact]
    public void CreateDraft_WithEmptyOrganizationId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                Guid.Empty,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                null));
    }

    [Fact]
    public void CreateDraft_WithEmptyCropCycleId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                Guid.Empty,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                null));
    }

    [Fact]
    public void CreateDraft_WithDefaultDate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                default,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-SET")]
    [InlineData("SET SPACE")]
    [InlineData("SET@001")]
    public void CreateDraft_WithInvalidCode_ShouldThrow(
        string code)
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                code,
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                null));
    }

    [Fact]
    public void CreateDraft_WithTooLongCode_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "S" + new string(
                    'E',
                    ProfitSharingSettlement.MaxCodeLength),
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-MITRA")]
    [InlineData("MITRA SPACE")]
    [InlineData("MITRA@001")]
    public void CreateDraft_WithInvalidManagingPartnerCode_ShouldThrow(
        string code)
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                code,
                "Mitra Pengelola",
                CreateCalculation(),
                null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateDraft_WithBlankManagingPartnerName_ShouldThrow(
        string name)
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                name,
                CreateCalculation(),
                null));
    }

    [Fact]
    public void CreateDraft_WithTooLongManagingPartnerName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                new string(
                    'M',
                    ProfitSharingSettlement
                        .MaxManagingPartnerNameLength +
                    1),
                CreateCalculation(),
                null));
    }

    [Fact]
    public void CreateDraft_WithMismatchedOrganization_ShouldThrow()
    {
        var calculation =
            CreateCalculation() with
            {
                OrganizationId = Guid.NewGuid()
            };

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                calculation,
                null));
    }

    [Fact]
    public void CreateDraft_WithMismatchedCropCycle_ShouldThrow()
    {
        var calculation =
            CreateCalculation() with
            {
                CropCycleId = Guid.NewGuid()
            };

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                calculation,
                null));
    }

    [Fact]
    public void CreateDraft_WithMissingManagingPartnerAllocation_ShouldThrow()
    {
        var calculation = CreateCalculation();

        var changedAllocations =
            calculation.Allocations
                .Where(allocation =>
                    allocation.ContributorCodeSnapshot !=
                        "MITRA-001")
                .ToArray();

        var changedCalculation =
            calculation with
            {
                Allocations = changedAllocations
            };

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                changedCalculation,
                null));
    }

    [Fact]
    public void CreateDraft_WithManagingPartnerNameConflict_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Nama Berbeda",
                CreateCalculation(),
                null));
    }

    [Fact]
    public void CreateDraft_WithUnsupportedCalculationVersion_ShouldThrow()
    {
        var calculation =
            CreateCalculation() with
            {
                CalculationVersion = "SIPACUL-PS-999"
            };

        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                calculation,
                null));
    }

    [Fact]
    public void CreateDraft_WithNullCalculation_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                null!,
                null));
    }

    [Fact]
    public void CreateDraft_WithTooLongNotes_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ProfitSharingSettlement.CreateDraft(
                OrganizationId,
                CropCycleId,
                "SET-001",
                SettlementDate,
                "MITRA-001",
                "Mitra Pengelola",
                CreateCalculation(),
                new string(
                    'N',
                    ProfitSharingSettlement.MaxNotesLength +
                    1)));
    }

    [Theory]
    [InlineData(1, ProfitSharingSettlementStatus.Draft)]
    [InlineData(2, ProfitSharingSettlementStatus.Finalized)]
    [InlineData(3, ProfitSharingSettlementStatus.Voided)]
    public void Status_ShouldUseStableNumericValues(
        int expected,
        ProfitSharingSettlementStatus status)
    {
        Assert.Equal(expected, (int)status);
    }

    private static ProfitSharingSettlement CreateSettlement(
        ProfitSharingCalculationResult? calculation = null)
    {
        return ProfitSharingSettlement.CreateDraft(
            OrganizationId,
            CropCycleId,
            "  set-001  ",
            SettlementDate,
            "  mitra-001  ",
            "  Mitra Pengelola  ",
            calculation ?? CreateCalculation(),
            "  Catatan settlement  ");
    }

    private static ProfitSharingCalculationResult
        CreateCalculation(
            decimal revenue = 600,
            decimal collectedRevenue = 600,
            decimal activityCost = 200,
            decimal manualCost = 100,
            decimal investorCapital = 200,
            decimal partnerCapital = 100,
            IReadOnlyCollection<ProfitSharingContributorInput>?
                contributors = null)
    {
        var resolvedContributors =
            contributors ??
            [
                Contributor(
                    "INV-001",
                    "Investor Utama",
                    CapitalContributorRole.Investor,
                    investorCapital),
                Contributor(
                    "MITRA-001",
                    "Mitra Pengelola",
                    CapitalContributorRole.Partner,
                    partnerCapital)
            ];

        var report =
            CropCycleProfitabilityReport.Calculate(
                new CropCycleProfitabilityInput(
                    OrganizationId,
                    CropCycleId,
                    "CC-001",
                    "Musim Padi",
                    CommodityId,
                    "PADI",
                    "Padi",
                    revenue,
                    collectedRevenue,
                    activityCost,
                    manualCost,
                    investorCapital,
                    partnerCapital,
                    0,
                    new DateTime(
                        2027,
                        7,
                        10,
                        8,
                        0,
                        0,
                        DateTimeKind.Utc)));

        return ProfitSharingCalculator.Calculate(
            report,
            "MITRA-001",
            "Mitra Pengelola",
            resolvedContributors);
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

    private static ProfitSharingAllocation Allocation(
        ProfitSharingSettlement settlement,
        CapitalContributorRole role,
        string code)
    {
        return settlement.Allocations.Single(allocation =>
            allocation.ContributorRole == role &&
            allocation.ContributorCodeSnapshot == code);
    }
}
