using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.Activities;
using SiPacul.Application.Cultivation.Activities.Contracts;
using SiPacul.Application.Cultivation.Activities.Persistence;
using SiPacul.Application.Cultivation.Activities.Services;
using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Services;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.Cultivation.Sops.Services;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Cultivation.Activities;

public sealed class CultivationActivityServiceTests
{
    private static readonly DateOnly PlannedDate =
        new(2027, 1, 5);

    private static readonly DateOnly CycleStart =
        new(2027, 1, 10);

    private static readonly DateOnly HarvestDate =
        new(2027, 5, 10);

    [Fact]
    public async Task Create_UnlinkedActivity_ShouldCreateAndSave()
    {
        var context = CreateContext();
        var activityRepository =
            new FakeActivityRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                activityRepository,
                unitOfWork)
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    cultivationSopId: null,
                    cultivationSopStepId: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "ACT-001",
            result.Value.Code);
        Assert.Equal(
            SopComplianceStatus.NotApplicable,
            result.Value.SopComplianceStatus);
        Assert.Single(activityRepository.Activities);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_LinkedActivity_ShouldSnapshotSopStep()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeActivityRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    context.Sop.Id,
                    context.Step.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            context.Step.Id,
            result.Value.CultivationSopStepId);
        Assert.Equal(
            context.Step.Name,
            result.Value.SopStepNameSnapshot);
        Assert.Equal(
            context.Step.Sequence,
            result.Value.SopStepSequenceSnapshot);
        Assert.Equal(
            SopComplianceStatus.NotEvaluated,
            result.Value.SopComplianceStatus);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldReturnConflict()
    {
        var context = CreateContext();

        var existing = CreateActivity(context);

        var result = await CreateService(
                context,
                new FakeActivityRepository(existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    context.Sop.Id,
                    context.Step.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors
                .CodeAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenPlannedDateAfterHarvest_ShouldFail()
    {
        var context = CreateContext();

        var request = CreateRequest(
            context.Sop.Id,
            context.Step.Id) with
        {
            PlannedDate =
                context.CropCycle
                    .ExpectedHarvestDate
                    .AddDays(1)
        };

        var result = await CreateService(
                context,
                new FakeActivityRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors
                .PlannedDateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCropCycleTerminal_ShouldFail()
    {
        var context = CreateContext();
        context.CropCycle.Cancel("Rencana dibatalkan");

        var result = await CreateService(
                context,
                new FakeActivityRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors
                .CropCycleTerminalCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenSopInactive_ShouldFail()
    {
        var context = CreateContext();
        context.Sop.Deactivate();

        var result = await CreateService(
                context,
                new FakeActivityRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    context.Sop.Id,
                    context.Step.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors.SopInactiveCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenStepDoesNotBelongToSop_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeActivityRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    context.Sop.Id,
                    Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors.SopStepMismatchCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldFilterAndIsolateCropCycle()
    {
        var context = CreateContext();
        var otherCycle = CreateCropCycle(
            context,
            "SC-002",
            CycleStart.AddMonths(6),
            HarvestDate.AddMonths(6));

        var expected = CreateActivity(context);
        expected.Start(PlannedDate);

        var planned = CreateActivity(
            context,
            "ACT-002");

        var other = CultivationActivity.Create(
            context.Organization.Id,
            otherCycle.Id,
            "ACT-OTHER",
            "Aktivitas Lain",
            CultivationActivityType.Other,
            PlannedDate,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        var result = await CreateService(
                context,
                new FakeActivityRepository(
                    expected,
                    planned,
                    other),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CultivationActivityFilter(
                    Status:
                        CultivationActivityStatus
                            .InProgress));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expected.Id, result.Value[0].Id);
    }

    [Fact]
    public async Task GetById_FromOtherCropCycle_ShouldReturnNotFound()
    {
        var context = CreateContext();
        var otherCycle = CreateCropCycle(
            context,
            "SC-002",
            CycleStart.AddMonths(6),
            HarvestDate.AddMonths(6));

        var activity = CultivationActivity.Create(
            context.Organization.Id,
            otherCycle.Id,
            "ACT-OTHER",
            "Aktivitas Lain",
            CultivationActivityType.Other,
            PlannedDate,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        var result = await CreateService(
                context,
                new FakeActivityRepository(activity),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdatePlan_WithValidRequest_ShouldUpdate()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeActivityRepository(activity),
                unitOfWork)
            .UpdatePlanAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                new UpdateCultivationActivityPlanRequest(
                    "  Persiapan Bedengan  ",
                    CultivationActivityType
                        .LandPreparation,
                    PlannedDate.AddDays(1),
                    "  Setelah hujan  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Persiapan Bedengan",
            result.Value.Name);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Start_CompleteLinkedActivity_ShouldSucceed()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);
        var repository =
            new FakeActivityRepository(activity);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            context,
            repository,
            unitOfWork);

        var startResult = await service.StartAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            activity.Id,
            new StartCultivationActivityRequest(
                PlannedDate));

        var completeResult = await service.CompleteAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            activity.Id,
            new CompleteCultivationActivityRequest(
                PlannedDate.AddDays(2),
                "Pekerjaan selesai",
                null,
                SopComplianceStatus.Compliant,
                null));

        Assert.True(startResult.IsSuccess);
        Assert.True(completeResult.IsSuccess);
        Assert.Equal(
            CultivationActivityStatus.Completed,
            completeResult.Value.Status);
        Assert.Equal(2, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cancel_ShouldStoreReason()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);

        var result = await CreateService(
                context,
                new FakeActivityRepository(activity),
                new FakeUnitOfWork())
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                new CancelCultivationActivityRequest(
                    "  Alat tidak tersedia  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Alat tidak tersedia",
            result.Value.CancellationReason);
    }

    [Fact]
    public async Task AddResource_ShouldCalculateTotalCost()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);

        var result = await CreateService(
                context,
                new FakeActivityRepository(activity),
                new FakeUnitOfWork())
            .AddResourceAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                new AddCultivationActivityResourceRequest(
                    CultivationResourceType.Material,
                    "Pupuk Urea",
                    100,
                    "kg",
                    4500,
                    null));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Resources);
        Assert.Equal(
            450000m,
            result.Value.TotalActualCost);
    }

    [Fact]
    public async Task UpdateAndRemoveResource_ShouldSucceed()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);

        var resource = activity.AddResource(
            CultivationResourceType.Equipment,
            "Traktor",
            2,
            "jam",
            200000,
            null);

        var service = CreateService(
            context,
            new FakeActivityRepository(activity),
            new FakeUnitOfWork());

        var updateResult =
            await service.UpdateResourceAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                resource.Id,
                new UpdateCultivationActivityResourceRequest(
                    "Traktor",
                    3,
                    "jam",
                    200000,
                    null));

        var removeResult =
            await service.RemoveResourceAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                resource.Id);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(
            600000m,
            updateResult.Value.TotalActualCost);
        Assert.True(removeResult.IsSuccess);
        Assert.Empty(removeResult.Value.Resources);
    }

    [Fact]
    public async Task UpdateMissingResource_ShouldReturnNotFound()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);

        var result = await CreateService(
                context,
                new FakeActivityRepository(activity),
                new FakeUnitOfWork())
            .UpdateResourceAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                Guid.NewGuid(),
                new UpdateCultivationActivityResourceRequest(
                    "Pupuk",
                    1,
                    "kg",
                    1000,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors.ResourceNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Mutation_WhenParentTerminal_ShouldReturnConflict()
    {
        var context = CreateContext();
        var activity = CreateActivity(context);
        context.CropCycle.Cancel("Siklus dibatalkan");

        var result = await CreateService(
                context,
                new FakeActivityRepository(activity),
                new FakeUnitOfWork())
            .AddResourceAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                activity.Id,
                new AddCultivationActivityResourceRequest(
                    CultivationResourceType.Material,
                    "Pupuk",
                    1,
                    "kg",
                    1000,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors
                .CropCycleTerminalCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CropCycleComplete_WithInProgressActivity_ShouldFail()
    {
        var context = CreateContext();
        context.CropCycle.Start(CycleStart);

        var activity = CreateActivity(context);
        activity.Start(PlannedDate);

        var activityRepository =
            new FakeActivityRepository(activity);

        var service = new CropCycleService(
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeSopRepository(context.Sop),
            new FakeLandRepository(context.Land),
            new FakeUnitOfWork(),
            activityRepository);

        var result = await service.CompleteAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            new CompleteCropCycleRequest(HarvestDate));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors
                .CropCycleHasInProgressActivitiesCode,
            result.Error.Code);
        Assert.Equal(
            CropCycleStatus.InProgress,
            context.CropCycle.Status);
    }

    [Fact]
    public async Task SopRemoveStep_WhenReferenced_ShouldFail()
    {
        var context = CreateContext();

        var activity = CreateActivity(context);

        var service = new CultivationSopService(
            new FakeSopRepository(context.Sop),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeUnitOfWork(),
            new FakeActivityRepository(activity));

        var result = await service.RemoveStepAsync(
            context.Organization.Id,
            context.Sop.Id,
            context.Step.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationActivityErrors
                .SopStepHistoricalReferenceExistsCode,
            result.Error.Code);
        Assert.Contains(
            context.Sop.Steps,
            step => step.Id == context.Step.Id);
    }

    private static CultivationActivityService CreateService(
        TestContext context,
        ICultivationActivityRepository
            activityRepository,
        IUnitOfWork unitOfWork)
    {
        return new CultivationActivityService(
            activityRepository,
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeSopRepository(context.Sop),
            unitOfWork);
    }

    private static TestContext CreateContext()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Organisasi Pertanian");

        var commodity = Commodity.Create(
            organization.Id,
            CommodityCode.Create("PADI"),
            "Padi",
            Guid.NewGuid(),
            null,
            null);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Budidaya Padi",
            null);

        var step = sop.AddStep(
            "Pengolahan Tanah",
            null,
            -14,
            3,
            true);

        var land = Land.Create(
            organization.Id,
            "LHN-001",
            "Lahan Utama",
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            null,
            null,
            null,
            null,
            null);

        var plot = land.AddPlot(
            "PTK-001",
            "Petak Utama",
            6000,
            AreaUnit.SquareMeter,
            null,
            null);

        var cropCycle = CropCycle.Create(
            organization.Id,
            "SC-001",
            "Musim Tanam Padi",
            commodity.Id,
            sop.Id,
            land.Id,
            plot.Id,
            5000,
            AreaUnit.SquareMeter,
            CycleStart,
            HarvestDate,
            null);

        return new TestContext(
            organization,
            commodity,
            sop,
            step,
            land,
            plot,
            cropCycle);
    }

    private static CreateCultivationActivityRequest
        CreateRequest(
            Guid? cultivationSopId = null,
            Guid? cultivationSopStepId = null)
    {
        return new CreateCultivationActivityRequest(
            "  act-001  ",
            "Pengolahan Lahan",
            CultivationActivityType.LandPreparation,
            PlannedDate,
            cultivationSopId,
            cultivationSopStepId,
            null);
    }

    private static CultivationActivity CreateActivity(
        TestContext context,
        string code = "ACT-001")
    {
        return CultivationActivity.Create(
            context.Organization.Id,
            context.CropCycle.Id,
            code,
            "Pengolahan Lahan",
            CultivationActivityType.LandPreparation,
            PlannedDate,
            context.Sop.Id,
            context.Step.Id,
            context.Step.Sequence,
            context.Step.Name,
            context.Step.PlannedDayOffset,
            context.Step.EstimatedDurationDays,
            context.Step.IsRequired,
            null);
    }

    private static CropCycle CreateCropCycle(
        TestContext context,
        string code,
        DateOnly start,
        DateOnly harvest)
    {
        return CropCycle.Create(
            context.Organization.Id,
            code,
            "Musim Tanam Lain",
            context.Commodity.Id,
            context.Sop.Id,
            context.Land.Id,
            context.Plot.Id,
            5000,
            AreaUnit.SquareMeter,
            start,
            harvest,
            null);
    }

    private sealed record TestContext(
        Organization Organization,
        Commodity Commodity,
        CultivationSop Sop,
        CultivationSopStep Step,
        Land Land,
        LandPlot Plot,
        CropCycle CropCycle);

    private sealed class FakeActivityRepository :
        ICultivationActivityRepository
    {
        private readonly List<CultivationActivity>
            _activities;

        public FakeActivityRepository(
            params CultivationActivity[] activities)
        {
            _activities = activities.ToList();
        }

        public IReadOnlyList<CultivationActivity>
            Activities => _activities;

        public Task<IReadOnlyList<CultivationActivity>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                CultivationActivityStatus? status = null,
                CultivationActivityType? activityType = null,
                DateOnly? plannedFrom = null,
                DateOnly? plannedTo = null,
                Guid? cultivationSopStepId = null,
                CancellationToken cancellationToken = default)
        {
            IEnumerable<CultivationActivity> query =
                _activities.Where(activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    !activity.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(activity =>
                    activity.Status == status.Value);
            }

            if (activityType.HasValue)
            {
                query = query.Where(activity =>
                    activity.ActivityType ==
                        activityType.Value);
            }

            if (plannedFrom.HasValue)
            {
                query = query.Where(activity =>
                    activity.PlannedDate >=
                        plannedFrom.Value);
            }

            if (plannedTo.HasValue)
            {
                query = query.Where(activity =>
                    activity.PlannedDate <=
                        plannedTo.Value);
            }

            if (cultivationSopStepId.HasValue)
            {
                query = query.Where(activity =>
                    activity.CultivationSopStepId ==
                        cultivationSopStepId.Value);
            }

            IReadOnlyList<CultivationActivity> result =
                query.ToArray();

            return Task.FromResult(result);
        }

        public Task<CultivationActivity?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    activityId));
        }

        public Task<CultivationActivity?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    activityId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _activities.Any(activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    activity.Code == code &&
                    !activity.IsDeleted));
        }

        public Task<bool> HasInProgressActivitiesAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _activities.Any(activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    activity.Status ==
                        CultivationActivityStatus
                            .InProgress &&
                    !activity.IsDeleted));
        }

        public Task<bool> HasAnyActivityForSopStepAsync(
            Guid organizationId,
            Guid cultivationSopId,
            Guid cultivationSopStepId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _activities.Any(activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CultivationSopId ==
                        cultivationSopId &&
                    activity.CultivationSopStepId ==
                        cultivationSopStepId &&
                    !activity.IsDeleted));
        }

        public void Add(CultivationActivity activity)
        {
            _activities.Add(activity);
        }

        private CultivationActivity? Find(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId)
        {
            return _activities.SingleOrDefault(activity =>
                activity.OrganizationId ==
                    organizationId &&
                activity.CropCycleId ==
                    cropCycleId &&
                activity.Id == activityId &&
                !activity.IsDeleted);
        }
    }

    private sealed class FakeCropCycleRepository :
        ICropCycleRepository
    {
        private readonly List<CropCycle> _cycles;

        public FakeCropCycleRepository(
            params CropCycle[] cycles)
        {
            _cycles = cycles.ToList();
        }

        public Task<IReadOnlyList<CropCycle>> GetAllAsync(
            Guid organizationId,
            CropCycleStatus? status = null,
            Guid? commodityId = null,
            Guid? landId = null,
            Guid? landPlotId = null,
            DateOnly? plannedStartFrom = null,
            DateOnly? plannedStartTo = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CropCycle> result =
                _cycles.Where(cycle =>
                    cycle.OrganizationId ==
                        organizationId &&
                    !cycle.IsDeleted)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<CropCycle?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(organizationId, cropCycleId));
        }

        public Task<CropCycle?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(organizationId, cropCycleId));
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
            _cycles.Add(cropCycle);
        }

        private CropCycle? Find(
            Guid organizationId,
            Guid cropCycleId)
        {
            return _cycles.SingleOrDefault(cycle =>
                cycle.OrganizationId ==
                    organizationId &&
                cycle.Id == cropCycleId &&
                !cycle.IsDeleted);
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
            IReadOnlyList<Organization> result =
                _organizations.ToArray();

            return Task.FromResult(result);
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

    private sealed class FakeCommodityRepository :
        ICommodityRepository
    {
        private readonly List<Commodity> _commodities;

        public FakeCommodityRepository(
            params Commodity[] commodities)
        {
            _commodities = commodities.ToList();
        }

        public Task<IReadOnlyList<Commodity>> GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Commodity> result =
                _commodities.Where(commodity =>
                    commodity.OrganizationId ==
                        organizationId)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<Commodity?> GetByIdAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _commodities.SingleOrDefault(
                    commodity =>
                        commodity.OrganizationId ==
                            organizationId &&
                        commodity.Id == commodityId));
        }

        public Task<Commodity?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid commodityId,
                CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                commodityId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            CommodityCode code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Commodity commodity)
        {
            _commodities.Add(commodity);
        }
    }

    private sealed class FakeSopRepository :
        ICultivationSopRepository
    {
        private readonly List<CultivationSop> _sops;

        public FakeSopRepository(
            params CultivationSop[] sops)
        {
            _sops = sops.ToList();
        }

        public Task<IReadOnlyList<CultivationSop>>
            GetAllAsync(
                Guid organizationId,
                Guid? commodityId = null,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CultivationSop> result =
                _sops.Where(sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    !sop.IsDeleted)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<CultivationSop?> GetByIdAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cultivationSopId));
        }

        public Task<CultivationSop?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cultivationSopId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cultivationSopId));
        }

        public Task<bool> NameExistsAsync(
            Guid organizationId,
            Guid commodityId,
            string name,
            Guid? excludedCultivationSopId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(CultivationSop cultivationSop)
        {
            _sops.Add(cultivationSop);
        }

        private CultivationSop? Find(
            Guid organizationId,
            Guid cultivationSopId)
        {
            return _sops.SingleOrDefault(sop =>
                sop.OrganizationId ==
                    organizationId &&
                sop.Id == cultivationSopId &&
                !sop.IsDeleted);
        }
    }

    private sealed class FakeLandRepository :
        ILandRepository
    {
        private readonly List<Land> _lands;

        public FakeLandRepository(params Land[] lands)
        {
            _lands = lands.ToList();
        }

        public Task<IReadOnlyList<Land>> GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Land> result =
                _lands.Where(land =>
                    land.OrganizationId ==
                        organizationId)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<Land?> GetByIdAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _lands.SingleOrDefault(land =>
                    land.OrganizationId ==
                        organizationId &&
                    land.Id == landId));
        }

        public Task<Land?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                landId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Land land)
        {
            _lands.Add(land);
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
