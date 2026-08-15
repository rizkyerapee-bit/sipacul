using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Tests.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallSettlementServiceTests
{
    private readonly Organization _organization =
        Organization.Create("ORG-001", "Organisasi Uji");

    private CropCycle CreateCropCycle()
    {
        return CropCycle.Create(
            _organization.Id,
            "CYCLE-001",
            "Siklus Uji",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1m,
            AreaUnit.Hectare,
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 6, 1),
            null);
    }

    [Fact]
    public async Task Finalize_WithNullRequest_ShouldReturnValidation()
    {
        var service = CreateService(CreateCropCycle());

        var result = await service.FinalizeAsync(
            _organization.Id,
            Guid.NewGuid(),
            null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Finalize_WhenActiveSettlementExists_ShouldConflict()
    {
        var cycle = CreateCropCycle();
        var processor = new FakeOperationProcessor
        {
            FinalizeResult =
                ProfitSharingWaterfallSettlementOperationResult.Failed(
                    ProfitSharingWaterfallSettlementFailure
                        .ActiveSettlementExists)
        };
        var service = CreateService(cycle, processor: processor);

        var result = await service.FinalizeAsync(
            _organization.Id,
            cycle.Id,
            new FinalizeProfitSharingWaterfallSettlementRequest(
                "SET-001",
                new DateOnly(2027, 7, 1),
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Contains(
            "ActiveSettlementExists",
            result.Error.Code);
        Assert.Equal(1, processor.FinalizeCallCount);
    }

    [Fact]
    public async Task GetAll_WithInvalidDateRange_ShouldValidate()
    {
        var cycle = CreateCropCycle();
        var repository = new FakeSettlementRepository();
        var service = CreateService(cycle, repository);

        var result = await service.GetAllAsync(
            _organization.Id,
            cycle.Id,
            new ProfitSharingWaterfallSettlementFilter(
                SettlementDateFrom: new DateOnly(2027, 8, 1),
                SettlementDateTo: new DateOnly(2027, 7, 1)));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, repository.GetAllCallCount);
    }

    [Fact]
    public async Task GetAll_ShouldPassFilterToRepository()
    {
        var cycle = CreateCropCycle();
        var repository = new FakeSettlementRepository();
        var service = CreateService(cycle, repository);

        var result = await service.GetAllAsync(
            _organization.Id,
            cycle.Id,
            new ProfitSharingWaterfallSettlementFilter(
                ProfitSharingWaterfallSettlementStatus.Voided,
                new DateOnly(2027, 7, 1),
                new DateOnly(2027, 7, 31)));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(1, repository.GetAllCallCount);
        Assert.Equal(
            ProfitSharingWaterfallSettlementStatus.Voided,
            repository.LastStatus);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var cycle = CreateCropCycle();
        var service = CreateService(cycle);

        var result = await service.GetByIdAsync(
            _organization.Id,
            cycle.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Void_WithBlankReason_ShouldReturnValidation()
    {
        var cycle = CreateCropCycle();
        var processor = new FakeOperationProcessor();
        var service = CreateService(cycle, processor: processor);

        var result = await service.VoidAsync(
            _organization.Id,
            cycle.Id,
            Guid.NewGuid(),
            new VoidProfitSharingWaterfallSettlementRequest(" "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, processor.VoidCallCount);
    }

    [Fact]
    public async Task Void_WhenSettlementMissing_ShouldReturnNotFound()
    {
        var cycle = CreateCropCycle();
        var processor = new FakeOperationProcessor
        {
            VoidResult =
                ProfitSharingWaterfallSettlementOperationResult.Failed(
                    ProfitSharingWaterfallSettlementFailure
                        .SettlementNotFound)
        };
        var service = CreateService(cycle, processor: processor);

        var result = await service.VoidAsync(
            _organization.Id,
            cycle.Id,
            Guid.NewGuid(),
            new VoidProfitSharingWaterfallSettlementRequest(
                "Koreksi sumber."));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(1, processor.VoidCallCount);
    }

    [Fact]
    public void AddApplication_ShouldRegisterServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = services.Single(service =>
            service.ServiceType ==
                typeof(IProfitSharingWaterfallSettlementService));

        Assert.Equal(
            typeof(ProfitSharingWaterfallSettlementService),
            descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private ProfitSharingWaterfallSettlementService CreateService(
        CropCycle cycle,
        FakeSettlementRepository? repository = null,
        FakeOperationProcessor? processor = null)
    {
        return new ProfitSharingWaterfallSettlementService(
            repository ?? new FakeSettlementRepository(),
            processor ?? new FakeOperationProcessor(),
            new FakeCropCycleRepository(cycle),
            new FakeOrganizationRepository(_organization));
    }

    private sealed class FakeOperationProcessor :
        IProfitSharingWaterfallSettlementOperationProcessor
    {
        public ProfitSharingWaterfallSettlementOperationResult
            FinalizeResult { get; set; } =
                ProfitSharingWaterfallSettlementOperationResult.Failed(
                    ProfitSharingWaterfallSettlementFailure
                        .ConcurrencyConflict);

        public ProfitSharingWaterfallSettlementOperationResult
            VoidResult { get; set; } =
                ProfitSharingWaterfallSettlementOperationResult.Failed(
                    ProfitSharingWaterfallSettlementFailure
                        .ConcurrencyConflict);

        public int FinalizeCallCount { get; private set; }

        public int VoidCallCount { get; private set; }

        public Task<ProfitSharingWaterfallSettlementOperationResult>
            FinalizeAsync(
                Guid organizationId,
                Guid cropCycleId,
                string code,
                DateOnly settlementDate,
                string? notes,
                CancellationToken cancellationToken = default)
        {
            FinalizeCallCount++;
            return Task.FromResult(FinalizeResult);
        }

        public Task<ProfitSharingWaterfallSettlementOperationResult>
            VoidAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                string voidReason,
                CancellationToken cancellationToken = default)
        {
            VoidCallCount++;
            return Task.FromResult(VoidResult);
        }
    }

    private sealed class FakeSettlementRepository :
        IProfitSharingWaterfallSettlementRepository
    {
        public int GetAllCallCount { get; private set; }

        public ProfitSharingWaterfallSettlementStatus? LastStatus
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<ProfitSharingWaterfallSettlement>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                ProfitSharingWaterfallSettlementStatus? status = null,
                DateOnly? settlementDateFrom = null,
                DateOnly? settlementDateTo = null,
                CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            LastStatus = status;
            return Task.FromResult<
                IReadOnlyList<ProfitSharingWaterfallSettlement>>([]);
        }

        public Task<ProfitSharingWaterfallSettlement?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProfitSharingWaterfallSettlement?>(null);

        public Task<ProfitSharingWaterfallSettlement?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<ProfitSharingWaterfallSettlement?>(null);

        public Task<ProfitSharingWaterfallSettlement?>
            GetActiveFinalizedAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<ProfitSharingWaterfallSettlement?>(null);

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Add(ProfitSharingWaterfallSettlement settlement)
        {
        }
    }

    private sealed class FakeOrganizationRepository :
        IOrganizationRepository
    {
        private readonly Organization _organization;

        public FakeOrganizationRepository(Organization organization)
        {
            _organization = organization;
        }

        public Task<IReadOnlyList<Organization>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Organization>>(
                [_organization]);

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Organization?>(
                organizationId == _organization.Id
                    ? _organization
                    : null);

        public Task<Organization?> GetByIdForUpdateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(organizationId, cancellationToken);

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Add(Organization organization)
        {
        }
    }

    private sealed class FakeCropCycleRepository : ICropCycleRepository
    {
        private readonly CropCycle _cycle;

        public FakeCropCycleRepository(CropCycle cycle)
        {
            _cycle = cycle;
        }

        public Task<IReadOnlyList<CropCycle>> GetAllAsync(
            Guid organizationId,
            CropCycleStatus? status = null,
            Guid? commodityId = null,
            Guid? landId = null,
            Guid? landPlotId = null,
            DateOnly? plannedStartFrom = null,
            DateOnly? plannedStartTo = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CropCycle>>([_cycle]);

        public Task<CropCycle?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CropCycle?>(
                organizationId == _cycle.OrganizationId &&
                cropCycleId == _cycle.Id
                    ? _cycle
                    : null);

        public Task<CropCycle?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasScheduleConflictAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            DateOnly plannedStartDate,
            DateOnly expectedHarvestDate,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasInProgressCycleAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasActiveCycleForLandAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasActiveCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasAnyCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Add(CropCycle cropCycle)
        {
        }
    }
}
