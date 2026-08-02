using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application;
using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.Profitability;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Finance.Profitability.Services;
using SiPacul.Application.Finance.ProfitSharing;
using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Finance.ProfitSharing;

public sealed class ProfitSharingSettlementServiceTests
{
    private static readonly DateOnly PlannedStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    private static readonly DateOnly SettlementDate =
        new(2027, 5, 20);

    [Fact]
    public async Task CreateDraft_WithValidData_ShouldPersist()
    {
        var context = CreateContext();
        var repository =
            new FakeSettlementRepository();

        var unitOfWork = new FakeUnitOfWork();

        var service =
            CreateService(
                context,
                repository,
                CreateContributions(context),
                CreateProfitabilityService(context),
                unitOfWork);

        var result = await service.CreateDraftAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Settlements);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal("SET-001", result.Value.Code);

        Assert.Equal(
            ProfitSharingSettlementStatus.Draft,
            result.Value.Status);

        Assert.Equal(2, result.Value.Allocations.Count);
        Assert.Equal(600m, result.Value.TotalPayout);
    }

    [Fact]
    public async Task CreateDraft_WithDuplicateCode_ShouldConflict()
    {
        var context = CreateContext();
        var existing = CreateSettlement(context);
        var unitOfWork = new FakeUnitOfWork();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(existing),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    unitOfWork)
                .CreateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .CodeAlreadyExistsCode,
            result.Error.Code);

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDraft_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var context = CreateContext();

        var service =
            new ProfitSharingSettlementService(
                new FakeSettlementRepository(),
                new FakeContributionRepository(),
                CreateProfitabilityService(context),
                new FakeCropCycleRepository(
                    context.CropCycle),
                new FakeOrganizationRepository(),
                new FakeUnitOfWork());

        var result = await service.CreateDraftAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CreateDraft_WhenCropCycleMissing_ShouldReturnNotFound()
    {
        var context = CreateContext();

        var service =
            new ProfitSharingSettlementService(
                new FakeSettlementRepository(),
                new FakeContributionRepository(),
                CreateProfitabilityService(context),
                new FakeCropCycleRepository(),
                new FakeOrganizationRepository(
                    context.Organization),
                new FakeUnitOfWork());

        var result = await service.CreateDraftAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .CropCycleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CreateDraft_WhenProfitabilityFails_ShouldForwardError()
    {
        var context = CreateContext();

        var profitabilityService =
            new FakeProfitabilityService(
                Result<CropCycleProfitabilityResponse>
                    .Failure(
                        ProfitabilityErrors
                            .SourceDataInvalid(
                                "Invalid source")));

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    CreateContributions(context),
                    profitabilityService,
                    new FakeUnitOfWork())
                .CreateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors.SourceDataInvalidCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CreateDraft_WithZeroCost_ShouldReturnConflict()
    {
        var context = CreateContext();

        var profitability =
            CreateProfitabilityResponse(
                context,
                recognizedRevenue: 0,
                collectedRevenue: 0,
                activityCost: 0,
                manualCost: 0,
                investorCapital: 0,
                partnerCapital: 0);

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    [],
                    new FakeProfitabilityService(
                        Result<
                            CropCycleProfitabilityResponse>
                            .Success(profitability)),
                    new FakeUnitOfWork())
                .CreateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .ZeroCostUnsupportedCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CreateDraft_WithUnbalancedCapital_ShouldReturnConflict()
    {
        var context = CreateContext();

        var profitability =
            CreateProfitabilityResponse(
                context,
                investorCapital: 150,
                partnerCapital: 100);

        var contributions =
            CreateContributions(
                context,
                investorAmount: 150,
                partnerAmount: 100);

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    contributions,
                    new FakeProfitabilityService(
                        Result<
                            CropCycleProfitabilityResponse>
                            .Success(profitability)),
                    new FakeUnitOfWork())
                .CreateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .CapitalDoesNotMatchCostCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CreateDraft_WhenContributionSnapshotChanged_ShouldConflict()
    {
        var context = CreateContext();

        var changedContributions =
            CreateContributions(
                context,
                investorAmount: 199,
                partnerAmount: 100);

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    changedContributions,
                    CreateProfitabilityService(context),
                    new FakeUnitOfWork())
                .CreateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .SourceDataChangedCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldForwardNormalizedFilter()
    {
        var context = CreateContext();
        var settlement = CreateSettlement(context);

        settlement.FinalizeSettlement();

        var repository =
            new FakeSettlementRepository(settlement);

        var service =
            CreateService(
                context,
                repository,
                CreateContributions(context),
                CreateProfitabilityService(context),
                new FakeUnitOfWork());

        var result = await service.GetAllAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            new ProfitSharingSettlementFilter(
                ProfitSharingSettlementStatus.Finalized,
                SettlementDate.AddDays(-1),
                SettlementDate.AddDays(1),
                "  mitra-001  "));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        Assert.Equal(
            ProfitSharingSettlementStatus.Finalized,
            repository.LastStatus);

        Assert.Equal(
            "MITRA-001",
            repository.LastManagingPartnerCode);
    }

    [Fact]
    public async Task GetAll_WithInvalidDateRange_ShouldReturnValidation()
    {
        var context = CreateContext();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    new FakeUnitOfWork())
                .GetAllAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    new ProfitSharingSettlementFilter(
                        SettlementDateFrom:
                            SettlementDate.AddDays(1),
                        SettlementDateTo:
                            SettlementDate));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var context = CreateContext();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    new FakeUnitOfWork())
                .GetByIdAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WithChangedData_ShouldSave()
    {
        var context = CreateContext();
        var settlement = CreateSettlement(context);
        var unitOfWork = new FakeUnitOfWork();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(
                        settlement),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    unitOfWork)
                .UpdateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    settlement.Id,
                    new UpdateProfitSharingSettlementRequest(
                        SettlementDate.AddDays(1),
                        "Catatan baru"));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            SettlementDate.AddDays(1),
            result.Value.SettlementDate);

        Assert.Equal("Catatan baru", result.Value.Notes);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WithSameData_ShouldNotSave()
    {
        var context = CreateContext();
        var settlement = CreateSettlement(context);
        var unitOfWork = new FakeUnitOfWork();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(
                        settlement),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    unitOfWork)
                .UpdateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    settlement.Id,
                    new UpdateProfitSharingSettlementRequest(
                        SettlementDate,
                        "Catatan"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WhenFinalized_ShouldReturnConflict()
    {
        var context = CreateContext();
        var settlement = CreateSettlement(context);

        settlement.FinalizeSettlement();

        var unitOfWork = new FakeUnitOfWork();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(
                        settlement),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    unitOfWork)
                .UpdateDraftAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    settlement.Id,
                    new UpdateProfitSharingSettlementRequest(
                        SettlementDate.AddDays(1),
                        null));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Void_Draft_ShouldSaveAndExposeReason()
    {
        var context = CreateContext();
        var settlement = CreateSettlement(context);
        var unitOfWork = new FakeUnitOfWork();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(
                        settlement),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    unitOfWork)
                .VoidAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    settlement.Id,
                    new VoidProfitSharingSettlementRequest(
                        "Koreksi sumber"));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            ProfitSharingSettlementStatus.Voided,
            result.Value.Status);

        Assert.Equal(
            "Koreksi sumber",
            result.Value.VoidReason);

        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Void_WhenAlreadyVoided_ShouldReturnConflict()
    {
        var context = CreateContext();
        var settlement = CreateSettlement(context);

        settlement.Void("Koreksi pertama");

        var unitOfWork = new FakeUnitOfWork();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(
                        settlement),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    unitOfWork)
                .VoidAsync(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    settlement.Id,
                    new VoidProfitSharingSettlementRequest(
                        "Koreksi kedua"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDraft_WithEmptyOrganizationId_ShouldValidate()
    {
        var context = CreateContext();

        var result =
            await CreateService(
                    context,
                    new FakeSettlementRepository(),
                    CreateContributions(context),
                    CreateProfitabilityService(context),
                    new FakeUnitOfWork())
                .CreateDraftAsync(
                    Guid.Empty,
                    context.CropCycle.Id,
                    CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitSharingSettlementErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public void AddApplication_ShouldRegisterServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor =
            services.Single(service =>
                service.ServiceType ==
                    typeof(
                        IProfitSharingSettlementService));

        Assert.Equal(
            typeof(ProfitSharingSettlementService),
            descriptor.ImplementationType);

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    private static ProfitSharingSettlementService
        CreateService(
            TestContext context,
            FakeSettlementRepository repository,
            IReadOnlyCollection<CapitalContribution>
                contributions,
            IProfitabilityService profitabilityService,
            IUnitOfWork unitOfWork)
    {
        return new ProfitSharingSettlementService(
            repository,
            new FakeContributionRepository(
                contributions.ToArray()),
            profitabilityService,
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization),
            unitOfWork);
    }

    private static TestContext CreateContext()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi Pertanian");

        var cropCycle =
            CropCycle.Create(
                organization.Id,
                "CC-001",
                "Musim Padi",
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                5000,
                AreaUnit.SquareMeter,
                PlannedStart,
                ExpectedHarvest,
                null);

        return new TestContext(
            organization,
            cropCycle);
    }

    private static CreateProfitSharingSettlementRequest
        CreateRequest()
    {
        return new CreateProfitSharingSettlementRequest(
            "  set-001  ",
            SettlementDate,
            "  mitra-001  ",
            "  Mitra Pengelola  ",
            "  Catatan  ");
    }

    private static IProfitabilityService
        CreateProfitabilityService(
            TestContext context)
    {
        return new FakeProfitabilityService(
            Result<CropCycleProfitabilityResponse>
                .Success(
                    CreateProfitabilityResponse(
                        context)));
    }

    private static CropCycleProfitabilityResponse
        CreateProfitabilityResponse(
            TestContext context,
            decimal recognizedRevenue = 600,
            decimal collectedRevenue = 600,
            decimal activityCost = 200,
            decimal manualCost = 100,
            decimal investorCapital = 200,
            decimal partnerCapital = 100)
    {
        var totalCost =
            activityCost + manualCost;

        var netProfit =
            recognizedRevenue - totalCost;

        var totalCapital =
            investorCapital + partnerCapital;

        return new CropCycleProfitabilityResponse(
            context.Organization.Id,
            context.CropCycle.Id,
            context.CropCycle.Code,
            context.CropCycle.Name,
            context.CropCycle.CommodityId,
            "PADI",
            "Padi",
            recognizedRevenue,
            collectedRevenue,
            recognizedRevenue - collectedRevenue,
            activityCost,
            manualCost,
            totalCost,
            netProfit,
            recognizedRevenue == 0
                ? null
                : Math.Round(
                    netProfit /
                    recognizedRevenue *
                    100m,
                    2,
                    MidpointRounding.AwayFromZero),
            netProfit switch
            {
                < 0 => ProfitabilityOutcome.Loss,
                > 0 => ProfitabilityOutcome.Profit,
                _ => ProfitabilityOutcome.BreakEven
            },
            investorCapital,
            partnerCapital,
            totalCapital,
            Math.Max(totalCost - totalCapital, 0),
            Math.Max(totalCapital - totalCost, 0),
            0,
            null,
            new DateTime(
                2027,
                5,
                20,
                8,
                0,
                0,
                DateTimeKind.Utc));
    }

    private static IReadOnlyCollection<CapitalContribution>
        CreateContributions(
            TestContext context,
            decimal investorAmount = 200,
            decimal partnerAmount = 100)
    {
        var investor =
            CapitalContribution.Create(
                context.Organization.Id,
                context.CropCycle.Id,
                "CAP-INV",
                PlannedStart,
                "INV-001",
                "Investor Utama",
                CapitalContributorRole.Investor,
                investorAmount,
                CapitalContributionPaymentMethod
                    .BankTransfer,
                null,
                null);

        investor.Confirm();

        var partner =
            CapitalContribution.Create(
                context.Organization.Id,
                context.CropCycle.Id,
                "CAP-MITRA",
                PlannedStart,
                "MITRA-001",
                "Mitra Pengelola",
                CapitalContributorRole.Partner,
                partnerAmount,
                CapitalContributionPaymentMethod.Cash,
                null,
                null);

        partner.Confirm();

        return [investor, partner];
    }

    private static ProfitSharingSettlement
        CreateSettlement(TestContext context)
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                new CropCycleProfitabilityInput(
                    context.Organization.Id,
                    context.CropCycle.Id,
                    context.CropCycle.Code,
                    context.CropCycle.Name,
                    context.CropCycle.CommodityId,
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
                        5,
                        20,
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
            context.Organization.Id,
            context.CropCycle.Id,
            "SET-001",
            SettlementDate,
            "MITRA-001",
            "Mitra Pengelola",
            calculation,
            "Catatan");
    }

    private sealed record TestContext(
        Organization Organization,
        CropCycle CropCycle);

    private sealed class FakeProfitabilityService :
        IProfitabilityService
    {
        private readonly
            Result<CropCycleProfitabilityResponse>
            _result;

        public FakeProfitabilityService(
            Result<CropCycleProfitabilityResponse>
                result)
        {
            _result = result;
        }

        public Task<
            Result<CropCycleProfitabilityResponse>>
            GetCropCycleReportAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeSettlementRepository :
        IProfitSharingSettlementRepository
    {
        public FakeSettlementRepository(
            params ProfitSharingSettlement[] settlements)
        {
            Settlements = settlements.ToList();
        }

        public List<ProfitSharingSettlement> Settlements
        {
            get;
        }

        public ProfitSharingSettlementStatus? LastStatus
        {
            get;
            private set;
        }

        public string? LastManagingPartnerCode
        {
            get;
            private set;
        }

        public Task<
            IReadOnlyList<
                ProfitSharingSettlement>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                ProfitSharingSettlementStatus? status = null,
                DateOnly? settlementDateFrom = null,
                DateOnly? settlementDateTo = null,
                string? managingPartnerCode = null,
                CancellationToken cancellationToken = default)
        {
            LastStatus = status;
            LastManagingPartnerCode =
                managingPartnerCode;

            IEnumerable<ProfitSharingSettlement> query =
                Settlements.Where(settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    !settlement.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(settlement =>
                    settlement.Status == status.Value);
            }

            if (settlementDateFrom.HasValue)
            {
                query = query.Where(settlement =>
                    settlement.SettlementDate >=
                        settlementDateFrom.Value);
            }

            if (settlementDateTo.HasValue)
            {
                query = query.Where(settlement =>
                    settlement.SettlementDate <=
                        settlementDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    managingPartnerCode))
            {
                query = query.Where(settlement =>
                    settlement.ManagingPartnerCode ==
                        managingPartnerCode);
            }

            return Task.FromResult(
                (IReadOnlyList<
                    ProfitSharingSettlement>)
                query
                    .OrderBy(settlement =>
                        settlement.SettlementDate)
                    .ThenBy(settlement =>
                        settlement.Code)
                    .ToArray());
        }

        public Task<ProfitSharingSettlement?>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    settlementId));
        }

        public Task<ProfitSharingSettlement?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                cropCycleId,
                settlementId,
                cancellationToken);
        }

        public Task<ProfitSharingSettlement?>
            GetActiveFinalizedAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Settlements.SingleOrDefault(settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !settlement.IsDeleted));
        }

        public Task<ProfitSharingSettlement?>
            GetActiveFinalizedForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return GetActiveFinalizedAsync(
                organizationId,
                cropCycleId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Settlements.Any(settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Code == code &&
                    !settlement.IsDeleted));
        }

        public Task<bool> HasActiveFinalizedAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid? excludedSettlementId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Settlements.Any(settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !settlement.IsDeleted &&
                    (
                        !excludedSettlementId.HasValue ||
                        settlement.Id !=
                            excludedSettlementId.Value
                    )));
        }

        public void Add(
            ProfitSharingSettlement settlement)
        {
            Settlements.Add(settlement);
        }

        private ProfitSharingSettlement? Find(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId)
        {
            return Settlements.SingleOrDefault(settlement =>
                settlement.OrganizationId ==
                    organizationId &&
                settlement.CropCycleId ==
                    cropCycleId &&
                settlement.Id == settlementId &&
                !settlement.IsDeleted);
        }
    }

    private sealed class FakeContributionRepository :
        ICapitalContributionRepository
    {
        private readonly List<CapitalContribution>
            _contributions;

        public FakeContributionRepository(
            params CapitalContribution[] contributions)
        {
            _contributions = contributions.ToList();
        }

        public Task<
            IReadOnlyList<CapitalContribution>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                CapitalContributionStatus? status = null,
                CapitalContributorRole? contributorRole = null,
                DateOnly? contributionDateFrom = null,
                DateOnly? contributionDateTo = null,
                string? contributorCode = null,
                string? contributorName = null,
                CancellationToken cancellationToken = default)
        {
            IEnumerable<CapitalContribution> query =
                _contributions.Where(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    !contribution.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(contribution =>
                    contribution.Status == status.Value);
            }

            if (contributorRole.HasValue)
            {
                query = query.Where(contribution =>
                    contribution.ContributorRole ==
                        contributorRole.Value);
            }

            return Task.FromResult(
                (IReadOnlyList<CapitalContribution>)
                query.ToArray());
        }

        public Task<CapitalContribution?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _contributions.SingleOrDefault(
                    contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.CropCycleId ==
                            cropCycleId &&
                        contribution.Id ==
                            contributionId &&
                        !contribution.IsDeleted));
        }

        public Task<CapitalContribution?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid contributionId,
                CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                cropCycleId,
                contributionId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<CapitalContribution?>
            GetContributorIdentityAsync(
                Guid organizationId,
                CapitalContributorRole contributorRole,
                string contributorCode,
                Guid? excludedContributionId = null,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _contributions.FirstOrDefault(
                    contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.ContributorRole ==
                            contributorRole &&
                        contribution.ContributorCode ==
                            contributorCode &&
                        (
                            !excludedContributionId.HasValue ||
                            contribution.Id !=
                                excludedContributionId.Value
                        ) &&
                        !contribution.IsDeleted));
        }

        public Task<CapitalContribution?>
            GetPartnerIdentityAsync(
                Guid organizationId,
                Guid? excludedContributionId = null,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _contributions.FirstOrDefault(
                    contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.ContributorRole ==
                            CapitalContributorRole.Partner &&
                        (
                            !excludedContributionId.HasValue ||
                            contribution.Id !=
                                excludedContributionId.Value
                        ) &&
                        !contribution.IsDeleted));
        }

        public void Add(
            CapitalContribution contribution)
        {
            _contributions.Add(contribution);
        }
    }

    private sealed class FakeCropCycleRepository :
        ICropCycleRepository
    {
        private readonly List<CropCycle> _cropCycles;

        public FakeCropCycleRepository(
            params CropCycle[] cropCycles)
        {
            _cropCycles = cropCycles.ToList();
        }

        public Task<IReadOnlyList<CropCycle>>
            GetAllAsync(
                Guid organizationId,
                CropCycleStatus? status = null,
                Guid? commodityId = null,
                Guid? landId = null,
                Guid? landPlotId = null,
                DateOnly? plannedStartFrom = null,
                DateOnly? plannedStartTo = null,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                (IReadOnlyList<CropCycle>)
                _cropCycles
                    .Where(cropCycle =>
                        cropCycle.OrganizationId ==
                            organizationId &&
                        !cropCycle.IsDeleted)
                    .ToArray());
        }

        public Task<CropCycle?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _cropCycles.SingleOrDefault(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.Id == cropCycleId &&
                    !cropCycle.IsDeleted));
        }

        public Task<CropCycle?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasScheduleConflictAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            DateOnly plannedStartDate,
            DateOnly expectedHarvestDate,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasInProgressCycleAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasActiveCycleForLandAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasActiveCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> HasAnyCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(CropCycle cropCycle)
        {
            _cropCycles.Add(cropCycle);
        }
    }

    private sealed class FakeOrganizationRepository :
        IOrganizationRepository
    {
        private readonly List<Organization>
            _organizations;

        public FakeOrganizationRepository(
            params Organization[] organizations)
        {
            _organizations = organizations.ToList();
        }

        public Task<IReadOnlyList<Organization>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                (IReadOnlyList<Organization>)
                _organizations.ToArray());
        }

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _organizations.SingleOrDefault(
                    organization =>
                        organization.Id ==
                            organizationId &&
                        !organization.IsDeleted));
        }

        public Task<Organization?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Organization organization)
        {
            _organizations.Add(organization);
        }
    }

    private sealed class FakeUnitOfWork :
        IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }
}
