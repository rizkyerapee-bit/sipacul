using SiPacul.Application.Finance.ProfitSharing;
using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Mappings;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using Xunit;

namespace SiPacul.Application.Tests.Finance.ProfitSharing;

public sealed class ProfitSharingSettlementContractTests
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
    public void ToResponse_ShouldMapIdentityAndLifecycle()
    {
        var settlement = CreateSettlement();

        var response = settlement.ToResponse();

        Assert.Equal(settlement.Id, response.Id);
        Assert.Equal(OrganizationId, response.OrganizationId);
        Assert.Equal(CropCycleId, response.CropCycleId);
        Assert.Equal("SET-001", response.Code);

        Assert.Equal(
            new DateOnly(2027, 7, 10),
            response.SettlementDate);

        Assert.Equal(
            "MITRA-001",
            response.ManagingPartnerCode);

        Assert.Equal(
            "Mitra Pengelola",
            response.ManagingPartnerName);

        Assert.Equal(
            ProfitSharingSettlementStatus.Draft,
            response.Status);

        Assert.False(response.IsActive);
        Assert.Null(response.FinalizedAt);
        Assert.Null(response.VoidedAt);
        Assert.Null(response.VoidReason);
        Assert.Equal("Catatan", response.Notes);
    }

    [Fact]
    public void ToResponse_ShouldMapFinancialSnapshot()
    {
        var response = CreateSettlement().ToResponse();

        Assert.Equal(600m, response.RecognizedRevenue);
        Assert.Equal(600m, response.CollectedRevenue);
        Assert.Equal(0m, response.OutstandingReceivable);
        Assert.Equal(200m, response.ActivityResourceCost);
        Assert.Equal(100m, response.ManualExpenseCost);
        Assert.Equal(300m, response.TotalCultivationCost);
        Assert.Equal(300m, response.NetProfit);

        Assert.Equal(
            ProfitabilityOutcome.Profit,
            response.Outcome);

        Assert.Equal(100m, response.ManagementProfitPool);
        Assert.Equal(200m, response.CapitalProfitPool);
        Assert.Equal(200m, response.TotalInvestorCapital);
        Assert.Equal(100m, response.TotalPartnerCapital);
        Assert.Equal(300m, response.TotalCapital);
        Assert.Equal(300m, response.TotalCapitalRecovery);
        Assert.Equal(0m, response.TotalCapitalLoss);
        Assert.Equal(133.33m, response.TotalInvestorProfitShare);
        Assert.Equal(166.67m, response.TotalPartnerProfitShare);
        Assert.Equal(600m, response.TotalPayout);

        Assert.Equal(
            ProfitSharingCalculator.CurrentCalculationVersion,
            response.CalculationVersion);
    }

    [Fact]
    public void ToResponse_ShouldMapOrderedAllocations()
    {
        var response = CreateSettlement().ToResponse();

        Assert.Collection(
            response.Allocations,
            investor =>
            {
                Assert.Equal(1, investor.Sequence);

                Assert.Equal(
                    CapitalContributorRole.Investor,
                    investor.ContributorRole);

                Assert.Equal(
                    "INV-001",
                    investor.ContributorCodeSnapshot);

                Assert.Equal(200m, investor.ConfirmedCapital);
                Assert.Equal(0.66666667m, investor.CapitalRatio);
                Assert.Equal(200m, investor.CapitalRecovery);
                Assert.Equal(0m, investor.CapitalLoss);
                Assert.Equal(0m, investor.ManagementProfitShare);
                Assert.Equal(133.33m, investor.CapitalProfitShare);
                Assert.Equal(133.33m, investor.TotalProfitShare);
                Assert.Equal(333.33m, investor.TotalPayout);
            },
            partner =>
            {
                Assert.Equal(2, partner.Sequence);

                Assert.Equal(
                    CapitalContributorRole.Partner,
                    partner.ContributorRole);

                Assert.Equal(
                    "MITRA-001",
                    partner.ContributorCodeSnapshot);

                Assert.Equal(100m, partner.ConfirmedCapital);
                Assert.Equal(0.33333333m, partner.CapitalRatio);
                Assert.Equal(100m, partner.CapitalRecovery);
                Assert.Equal(0m, partner.CapitalLoss);
                Assert.Equal(100m, partner.ManagementProfitShare);
                Assert.Equal(66.67m, partner.CapitalProfitShare);
                Assert.Equal(166.67m, partner.TotalProfitShare);
                Assert.Equal(266.67m, partner.TotalPayout);
            });
    }

    [Fact]
    public void ToResponse_AllocationsShouldBeReadOnly()
    {
        var response = CreateSettlement().ToResponse();

        var list =
            Assert.IsAssignableFrom<
                IList<ProfitSharingAllocationResponse>>(
                    response.Allocations);

        Assert.True(list.IsReadOnly);
    }

    [Fact]
    public void ToResponse_FinalizedSettlement_ShouldBeActive()
    {
        var settlement = CreateSettlement();

        settlement.FinalizeSettlement();

        var response = settlement.ToResponse();

        Assert.Equal(
            ProfitSharingSettlementStatus.Finalized,
            response.Status);

        Assert.True(response.IsActive);
        Assert.NotNull(response.FinalizedAt);
        Assert.Null(response.VoidedAt);
    }

    [Fact]
    public void ToResponse_VoidedSettlement_ShouldExposeReason()
    {
        var settlement = CreateSettlement();

        settlement.FinalizeSettlement();
        settlement.Void("Settlement diganti");

        var response = settlement.ToResponse();

        Assert.Equal(
            ProfitSharingSettlementStatus.Voided,
            response.Status);

        Assert.False(response.IsActive);
        Assert.NotNull(response.FinalizedAt);
        Assert.NotNull(response.VoidedAt);
        Assert.Equal(
            "Settlement diganti",
            response.VoidReason);
    }

    [Fact]
    public void SettlementMapping_WithNull_ShouldThrow()
    {
        ProfitSharingSettlement settlement = null!;

        Assert.Throws<ArgumentNullException>(() =>
            settlement.ToResponse());
    }

    [Fact]
    public void AllocationMapping_WithNull_ShouldThrow()
    {
        ProfitSharingAllocation allocation = null!;

        Assert.Throws<ArgumentNullException>(() =>
            allocation.ToResponse());
    }

    [Fact]
    public void RequestContracts_ShouldPreserveValues()
    {
        var create =
            new CreateProfitSharingSettlementRequest(
                "SET-001",
                new DateOnly(2027, 7, 10),
                "MITRA-001",
                "Mitra Pengelola",
                "Catatan");

        var update =
            new UpdateProfitSharingSettlementRequest(
                new DateOnly(2027, 7, 11),
                "Diperbarui");

        var voidRequest =
            new VoidProfitSharingSettlementRequest(
                "Koreksi");

        Assert.Equal("SET-001", create.Code);

        Assert.Equal(
            "MITRA-001",
            create.ManagingPartnerCode);

        Assert.Equal(
            new DateOnly(2027, 7, 11),
            update.SettlementDate);

        Assert.Equal("Koreksi", voidRequest.VoidReason);
    }

    [Fact]
    public void FilterContract_ShouldPreserveValues()
    {
        var filter =
            new ProfitSharingSettlementFilter(
                ProfitSharingSettlementStatus.Finalized,
                new DateOnly(2027, 7, 1),
                new DateOnly(2027, 7, 31),
                "MITRA-001");

        Assert.Equal(
            ProfitSharingSettlementStatus.Finalized,
            filter.Status);

        Assert.Equal(
            new DateOnly(2027, 7, 1),
            filter.SettlementDateFrom);

        Assert.Equal(
            new DateOnly(2027, 7, 31),
            filter.SettlementDateTo);

        Assert.Equal(
            "MITRA-001",
            filter.ManagingPartnerCode);
    }

    [Fact]
    public void ErrorCodes_ShouldRemainStable()
    {
        var expected = new[]
        {
            "ProfitSharingSettlements.Validation",
            "ProfitSharingSettlements.OrganizationNotFound",
            "ProfitSharingSettlements.CropCycleNotFound",
            "ProfitSharingSettlements.NotFound",
            "ProfitSharingSettlements.CodeAlreadyExists",
            "ProfitSharingSettlements.ActiveSettlementExists",
            "ProfitSharingSettlements.CropCycleNotTerminal",
            "ProfitSharingSettlements.ActiveActivityExists",
            "ProfitSharingSettlements.DraftHarvestExists",
            "ProfitSharingSettlements.UnsoldHarvestExists",
            "ProfitSharingSettlements.DraftSaleExists",
            "ProfitSharingSettlements.OutstandingReceivableExists",
            "ProfitSharingSettlements.DraftExpenseExists",
            "ProfitSharingSettlements.DraftContributionExists",
            "ProfitSharingSettlements.DraftPaymentExists",
            "ProfitSharingSettlements.CapitalDoesNotMatchCost",
            "ProfitSharingSettlements.ZeroCostUnsupported",
            "ProfitSharingSettlements.SourceDataChanged",
            "ProfitSharingSettlements.InvalidStatusTransition",
            "ProfitSharingSettlements.ConcurrencyConflict"
        };

        var actual = new[]
        {
            ProfitSharingSettlementErrors.ValidationCode,
            ProfitSharingSettlementErrors.OrganizationNotFoundCode,
            ProfitSharingSettlementErrors.CropCycleNotFoundCode,
            ProfitSharingSettlementErrors.NotFoundCode,
            ProfitSharingSettlementErrors.CodeAlreadyExistsCode,
            ProfitSharingSettlementErrors.ActiveSettlementExistsCode,
            ProfitSharingSettlementErrors.CropCycleNotTerminalCode,
            ProfitSharingSettlementErrors.ActiveActivityExistsCode,
            ProfitSharingSettlementErrors.DraftHarvestExistsCode,
            ProfitSharingSettlementErrors.UnsoldHarvestExistsCode,
            ProfitSharingSettlementErrors.DraftSaleExistsCode,
            ProfitSharingSettlementErrors
                .OutstandingReceivableExistsCode,
            ProfitSharingSettlementErrors.DraftExpenseExistsCode,
            ProfitSharingSettlementErrors
                .DraftContributionExistsCode,
            ProfitSharingSettlementErrors.DraftPaymentExistsCode,
            ProfitSharingSettlementErrors
                .CapitalDoesNotMatchCostCode,
            ProfitSharingSettlementErrors.ZeroCostUnsupportedCode,
            ProfitSharingSettlementErrors.SourceDataChangedCode,
            ProfitSharingSettlementErrors
                .InvalidStatusTransitionCode,
            ProfitSharingSettlementErrors.ConcurrencyConflictCode
        };

        Assert.Equal(expected, actual);
        Assert.Equal(20, actual.Distinct().Count());
    }

    [Fact]
    public void ErrorFactories_ShouldUseExpectedCodes()
    {
        Assert.Equal(
            ProfitSharingSettlementErrors.ValidationCode,
            ProfitSharingSettlementErrors
                .Validation("Invalid")
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.OrganizationNotFoundCode,
            ProfitSharingSettlementErrors
                .OrganizationNotFound(OrganizationId)
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.CropCycleNotFoundCode,
            ProfitSharingSettlementErrors
                .CropCycleNotFound(CropCycleId)
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.NotFoundCode,
            ProfitSharingSettlementErrors
                .NotFound(Guid.NewGuid())
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.CodeAlreadyExistsCode,
            ProfitSharingSettlementErrors
                .CodeAlreadyExists("SET-001")
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.SourceDataChangedCode,
            ProfitSharingSettlementErrors
                .SourceDataChanged()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.ConcurrencyConflictCode,
            ProfitSharingSettlementErrors
                .ConcurrencyConflict()
                .Code);
    }

    [Fact]
    public void FinalizationErrors_ShouldUseExpectedCodes()
    {
        Assert.Equal(
            ProfitSharingSettlementErrors.ActiveSettlementExistsCode,
            ProfitSharingSettlementErrors
                .ActiveSettlementExists(CropCycleId)
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.CropCycleNotTerminalCode,
            ProfitSharingSettlementErrors
                .CropCycleNotTerminal()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.ActiveActivityExistsCode,
            ProfitSharingSettlementErrors
                .ActiveActivityExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.DraftHarvestExistsCode,
            ProfitSharingSettlementErrors
                .DraftHarvestExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.UnsoldHarvestExistsCode,
            ProfitSharingSettlementErrors
                .UnsoldHarvestExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.DraftSaleExistsCode,
            ProfitSharingSettlementErrors
                .DraftSaleExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .OutstandingReceivableExistsCode,
            ProfitSharingSettlementErrors
                .OutstandingReceivableExists(100)
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.DraftExpenseExistsCode,
            ProfitSharingSettlementErrors
                .DraftExpenseExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .DraftContributionExistsCode,
            ProfitSharingSettlementErrors
                .DraftContributionExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.DraftPaymentExistsCode,
            ProfitSharingSettlementErrors
                .DraftPaymentExists()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .CapitalDoesNotMatchCostCode,
            ProfitSharingSettlementErrors
                .CapitalDoesNotMatchCost(100, 200)
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors.ZeroCostUnsupportedCode,
            ProfitSharingSettlementErrors
                .ZeroCostUnsupported()
                .Code);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .InvalidStatusTransitionCode,
            ProfitSharingSettlementErrors
                .InvalidStatusTransition("Invalid")
                .Code);
    }

    private static ProfitSharingSettlement CreateSettlement()
    {
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
                    600,
                    600,
                    200,
                    100,
                    200,
                    100,
                    0,
                    new DateTime(
                        2027,
                        7,
                        10,
                        8,
                        0,
                        0,
                        DateTimeKind.Utc)));

        var calculation =
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    new ProfitSharingContributorInput(
                        "INV-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        200),
                    new ProfitSharingContributorInput(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        100)
                ]);

        return ProfitSharingSettlement.CreateDraft(
            OrganizationId,
            CropCycleId,
            "SET-001",
            new DateOnly(2027, 7, 10),
            "MITRA-001",
            "Mitra Pengelola",
            calculation,
            "Catatan");
    }
}
