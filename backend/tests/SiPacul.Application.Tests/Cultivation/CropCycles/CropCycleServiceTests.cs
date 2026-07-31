using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles;
using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Services;
using SiPacul.Application.Cultivation.Sops.Persistence;
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

namespace SiPacul.Application.Tests.Cultivation.CropCycles;

public sealed class CropCycleServiceTests
{
    private static readonly DateOnly PlannedStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateAndSave()
    {
        var context = CreateReferenceContext();
        var repository = new FakeCropCycleRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            repository,
            context,
            unitOfWork);

        var result = await service.CreateAsync(
            context.Organization.Id,
            CreateRequest(
                context,
                code: "  sc-padi-001  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "SC-PADI-001",
            result.Value.Code);
        Assert.Equal(
            CropCycleStatus.Planned,
            result.Value.Status);
        Assert.Equal(
            5_000m,
            result.Value.PlantedAreaInSquareMeters);
        Assert.Single(repository.CropCycles);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var context = CreateReferenceContext();

        var service = new CropCycleService(
            new FakeCropCycleRepository(),
            new FakeOrganizationRepository(),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeCultivationSopRepository(
                context.Sop),
            new FakeLandRepository(
                context.Land),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            context.Organization.Id,
            CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCommodityMissing_ShouldReturnNotFound()
    {
        var context = CreateReferenceContext();

        var service = new CropCycleService(
            new FakeCropCycleRepository(),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeCommodityRepository(),
            new FakeCultivationSopRepository(
                context.Sop),
            new FakeLandRepository(
                context.Land),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            context.Organization.Id,
            CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.CommodityNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCommodityInactive_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        context.Commodity.Deactivate();

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.CommodityInactiveCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
    }

    [Fact]
    public async Task Create_WhenLandInactive_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        context.Land.Deactivate();

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.LandInactiveCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenPlotMissing_ShouldReturnNotFound()
    {
        var context = CreateReferenceContext();

        var request = new CreateCropCycleRequest(
            "SC-PADI-001",
            "Musim Tanam Padi",
            context.Commodity.Id,
            context.Sop.Id,
            context.Land.Id,
            Guid.NewGuid(),
            5_000,
            AreaUnit.SquareMeter,
            PlannedStart,
            ExpectedHarvest,
            null);

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.PlotNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenPlotInactive_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        context.Land.DeactivatePlot(
            context.Plot.Id);

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.PlotInactiveCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenSopMissing_ShouldReturnNotFound()
    {
        var context = CreateReferenceContext();

        var service = new CropCycleService(
            new FakeCropCycleRepository(),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeCultivationSopRepository(),
            new FakeLandRepository(
                context.Land),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            context.Organization.Id,
            CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.SopNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenSopInactive_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        context.Sop.Deactivate();

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.SopInactiveCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenSopCommodityDiffers_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();

        var otherSop = CultivationSop.Create(
            context.Organization.Id,
            Guid.NewGuid(),
            "SOP Komoditas Lain",
            null);

        var service = new CropCycleService(
            new FakeCropCycleRepository(),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeCultivationSopRepository(
                otherSop),
            new FakeLandRepository(
                context.Land),
            new FakeUnitOfWork());

        var request = CreateRequest(
            context,
            cultivationSopId: otherSop.Id);

        var result = await service.CreateAsync(
            context.Organization.Id,
            request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.SopCommodityMismatchCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WithoutSop_ShouldBeAllowed()
    {
        var context = CreateReferenceContext();

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(
                    context,
                    cultivationSopId: null,
                    useDefaultSop: false));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CultivationSopId);
    }

    [Fact]
    public async Task Create_WhenAreaExceedsPlot_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(
                    context,
                    plantedArea: 8_000));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.AreaCapacityExceededCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();

        var existing = CreateCycle(
            context,
            code: "SC-PADI-001",
            start: new DateOnly(2026, 1, 1),
            harvest: new DateOnly(2026, 5, 1));

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(existing),
                context,
                unitOfWork)
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(context));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.CodeAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithOverlappingSchedule_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();

        var existing = CreateCycle(
            context,
            start: PlannedStart.AddDays(-20),
            harvest: PlannedStart.AddDays(20));

        var result = await CreateService(
                new FakeCropCycleRepository(existing),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(
                    context,
                    code: "SC-PADI-002"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.ScheduleConflictCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenExistingCycleCancelled_ShouldAllowOverlap()
    {
        var context = CreateReferenceContext();

        var existing = CreateCycle(context);
        existing.Cancel("Rencana lama dibatalkan");

        var result = await CreateService(
                new FakeCropCycleRepository(existing),
                context,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                CreateRequest(
                    context,
                    code: "SC-PADI-002"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetAll_ShouldFilterAndIsolateOrganization()
    {
        var context = CreateReferenceContext();

        var otherContext = CreateReferenceContext(
            "ORG-002",
            "Organisasi Lain");

        var planned = CreateCycle(
            context,
            code: "SC-001");

        var inProgress = CreateCycle(
            context,
            code: "SC-002",
            start: PlannedStart.AddMonths(6),
            harvest: ExpectedHarvest.AddMonths(6));

        inProgress.Start(
            PlannedStart.AddMonths(6));

        var other = CreateCycle(
            otherContext,
            code: "SC-OTHER");

        var repository =
            new FakeCropCycleRepository(
                planned,
                inProgress,
                other);

        var result = await CreateService(
                repository,
                context,
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                new CropCycleFilter(
                    Status: CropCycleStatus.InProgress,
                    CommodityId: context.Commodity.Id));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(
            inProgress.Id,
            result.Value[0].Id);
    }

    [Fact]
    public async Task GetAll_WithInvalidDateFilter_ShouldReturnValidation()
    {
        var context = CreateReferenceContext();

        var result = await CreateService(
                new FakeCropCycleRepository(),
                context,
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                new CropCycleFilter(
                    PlannedStartFrom:
                        new DateOnly(2027, 5, 1),
                    PlannedStartTo:
                        new DateOnly(2027, 1, 1)));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
    }

    [Fact]
    public async Task GetById_FromOtherOrganization_ShouldReturnNotFound()
    {
        var context = CreateReferenceContext();

        var otherContext = CreateReferenceContext(
            "ORG-002",
            "Organisasi Lain");

        var otherCycle = CreateCycle(otherContext);

        var result = await CreateService(
                new FakeCropCycleRepository(
                    otherCycle),
                context,
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                otherCycle.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdatePlan_WithValidRequest_ShouldUpdateAndSave()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .UpdatePlanAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCyclePlanRequest(
                    "  Musim Tanam Organik  ",
                    null,
                    0.4m,
                    AreaUnit.Hectare,
                    PlannedStart.AddDays(5),
                    ExpectedHarvest.AddDays(5),
                    "  Fokus organik  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Musim Tanam Organik",
            result.Value.Name);
        Assert.Equal(
            4_000m,
            result.Value.PlantedAreaInSquareMeters);
        Assert.Null(result.Value.CultivationSopId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdatePlan_WithUnchangedRequest_ShouldNotSave()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .UpdatePlanAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCyclePlanRequest(
                    "  Musim Tanam Padi  ",
                    context.Sop.Id,
                    5_000,
                    AreaUnit.SquareMeter,
                    PlannedStart,
                    ExpectedHarvest,
                    "   "));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdatePlan_WithScheduleConflict_ShouldNotMutate()
    {
        var context = CreateReferenceContext();

        var cropCycle = CreateCycle(
            context,
            code: "SC-001");

        var other = CreateCycle(
            context,
            code: "SC-002",
            start: PlannedStart.AddMonths(6),
            harvest: ExpectedHarvest.AddMonths(6));

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle,
                    other),
                context,
                new FakeUnitOfWork())
            .UpdatePlanAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCyclePlanRequest(
                    "Nama Baru",
                    context.Sop.Id,
                    5_000,
                    AreaUnit.SquareMeter,
                    PlannedStart.AddMonths(7),
                    ExpectedHarvest.AddMonths(7),
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.ScheduleConflictCode,
            result.Error.Code);
        Assert.Equal(
            "Musim Tanam Padi",
            cropCycle.Name);
    }

    [Fact]
    public async Task UpdatePlan_AfterStart_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        cropCycle.Start(PlannedStart);

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                new FakeUnitOfWork())
            .UpdatePlanAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCyclePlanRequest(
                    "Nama Baru",
                    context.Sop.Id,
                    5_000,
                    AreaUnit.SquareMeter,
                    PlannedStart,
                    ExpectedHarvest,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Start_FromPlanned_ShouldStartAndSave()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .StartAsync(
                context.Organization.Id,
                cropCycle.Id,
                new StartCropCycleRequest(
                    PlannedStart));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            CropCycleStatus.InProgress,
            result.Value.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Start_WhenAnotherCycleInProgress_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();

        var planned = CreateCycle(
            context,
            code: "SC-001");

        var active = CreateCycle(
            context,
            code: "SC-002",
            start: PlannedStart.AddMonths(-6),
            harvest: ExpectedHarvest.AddMonths(-6));

        active.Start(
            PlannedStart.AddMonths(-6));

        var result = await CreateService(
                new FakeCropCycleRepository(
                    planned,
                    active),
                context,
                new FakeUnitOfWork())
            .StartAsync(
                context.Organization.Id,
                planned.Id,
                new StartCropCycleRequest(
                    PlannedStart));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.ActiveCycleAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Start_WhenLandBecomesInactive_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        context.Land.Deactivate();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                new FakeUnitOfWork())
            .StartAsync(
                context.Organization.Id,
                cropCycle.Id,
                new StartCropCycleRequest(
                    PlannedStart));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.LandInactiveCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Complete_FromInProgress_ShouldCompleteAndSave()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        cropCycle.Start(PlannedStart);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .CompleteAsync(
                context.Organization.Id,
                cropCycle.Id,
                new CompleteCropCycleRequest(
                    ExpectedHarvest));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            CropCycleStatus.Completed,
            result.Value.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Complete_FromPlanned_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                new FakeUnitOfWork())
            .CompleteAsync(
                context.Organization.Id,
                cropCycle.Id,
                new CompleteCropCycleRequest(
                    ExpectedHarvest));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromPlanned_ShouldCancelAndSave()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .CancelAsync(
                context.Organization.Id,
                cropCycle.Id,
                new CancelCropCycleRequest(
                    "  Perubahan rencana  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            CropCycleStatus.Cancelled,
            result.Value.Status);
        Assert.Equal(
            "Perubahan rencana",
            result.Value.CancellationReason);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cancel_FromCompleted_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        cropCycle.Start(PlannedStart);
        cropCycle.Complete(ExpectedHarvest);

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                new FakeUnitOfWork())
            .CancelAsync(
                context.Organization.Id,
                cropCycle.Id,
                new CancelCropCycleRequest(
                    "Tidak berlaku"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateNotes_WhilePlanned_ShouldSave()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .UpdateNotesAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCycleNotesRequest(
                    "  Catatan baru  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Catatan baru",
            result.Value.Notes);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateNotes_WithSameValue_ShouldNotSave()
    {
        var context = CreateReferenceContext();

        var cropCycle = CropCycle.Create(
            context.Organization.Id,
            "SC-PADI-001",
            "Musim Tanam Padi",
            context.Commodity.Id,
            context.Sop.Id,
            context.Land.Id,
            context.Plot.Id,
            5_000,
            AreaUnit.SquareMeter,
            PlannedStart,
            ExpectedHarvest,
            "Catatan");

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                unitOfWork)
            .UpdateNotesAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCycleNotesRequest(
                    "  Catatan  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateNotes_AfterCompletion_ShouldReturnConflict()
    {
        var context = CreateReferenceContext();
        var cropCycle = CreateCycle(context);
        cropCycle.Start(PlannedStart);
        cropCycle.Complete(ExpectedHarvest);

        var result = await CreateService(
                new FakeCropCycleRepository(
                    cropCycle),
                context,
                new FakeUnitOfWork())
            .UpdateNotesAsync(
                context.Organization.Id,
                cropCycle.Id,
                new UpdateCropCycleNotesRequest(
                    "Tidak boleh"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.InvalidStatusTransitionCode,
            result.Error.Code);
    }

    private static CropCycleService CreateService(
        ICropCycleRepository cropCycleRepository,
        ReferenceContext context,
        IUnitOfWork unitOfWork)
    {
        return new CropCycleService(
            cropCycleRepository,
            new FakeOrganizationRepository(
                context.Organization),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeCultivationSopRepository(
                context.Sop),
            new FakeLandRepository(
                context.Land),
            unitOfWork);
    }

    private static ReferenceContext CreateReferenceContext(
        string organizationCode = "ORG-001",
        string organizationName =
            "Organisasi Pertanian")
    {
        var organization = Organization.Create(
            organizationCode,
            organizationName);

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
            6_000,
            AreaUnit.SquareMeter,
            null,
            null);

        return new ReferenceContext(
            organization,
            commodity,
            sop,
            land,
            plot);
    }

    private static CreateCropCycleRequest CreateRequest(
        ReferenceContext context,
        string code = "SC-PADI-001",
        Guid? cultivationSopId = null,
        decimal plantedArea = 5_000,
        bool useDefaultSop = true)
    {
        var resolvedSopId =
            cultivationSopId ??
            (
                useDefaultSop
                    ? context.Sop.Id
                    : null
            );

        return new CreateCropCycleRequest(
            code,
            "Musim Tanam Padi",
            context.Commodity.Id,
            resolvedSopId,
            context.Land.Id,
            context.Plot.Id,
            plantedArea,
            AreaUnit.SquareMeter,
            PlannedStart,
            ExpectedHarvest,
            null);
    }

    private static CropCycle CreateCycle(
        ReferenceContext context,
        string code = "SC-PADI-001",
        DateOnly? start = null,
        DateOnly? harvest = null)
    {
        return CropCycle.Create(
            context.Organization.Id,
            code,
            "Musim Tanam Padi",
            context.Commodity.Id,
            context.Sop.Id,
            context.Land.Id,
            context.Plot.Id,
            5_000,
            AreaUnit.SquareMeter,
            start ?? PlannedStart,
            harvest ?? ExpectedHarvest,
            null);
    }

    private sealed record ReferenceContext(
        Organization Organization,
        Commodity Commodity,
        CultivationSop Sop,
        Land Land,
        LandPlot Plot);

    private sealed class FakeCropCycleRepository :
        ICropCycleRepository
    {
        private readonly List<CropCycle> _cropCycles;

        public FakeCropCycleRepository(
            params CropCycle[] cropCycles)
        {
            _cropCycles = cropCycles.ToList();
        }

        public IReadOnlyList<CropCycle> CropCycles =>
            _cropCycles;

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
            IEnumerable<CropCycle> query =
                _cropCycles.Where(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    !cropCycle.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(cropCycle =>
                    cropCycle.Status == status.Value);
            }

            if (commodityId.HasValue)
            {
                query = query.Where(cropCycle =>
                    cropCycle.CommodityId ==
                        commodityId.Value);
            }

            if (landId.HasValue)
            {
                query = query.Where(cropCycle =>
                    cropCycle.LandId == landId.Value);
            }

            if (landPlotId.HasValue)
            {
                query = query.Where(cropCycle =>
                    cropCycle.LandPlotId ==
                        landPlotId.Value);
            }

            if (plannedStartFrom.HasValue)
            {
                query = query.Where(cropCycle =>
                    cropCycle.PlannedStartDate >=
                        plannedStartFrom.Value);
            }

            if (plannedStartTo.HasValue)
            {
                query = query.Where(cropCycle =>
                    cropCycle.PlannedStartDate <=
                        plannedStartTo.Value);
            }

            IReadOnlyList<CropCycle> result =
                query
                    .OrderBy(cropCycle =>
                        cropCycle.PlannedStartDate)
                    .ThenBy(cropCycle =>
                        cropCycle.Code)
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
            return Task.FromResult(
                _cropCycles.Any(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.Code == code &&
                    !cropCycle.IsDeleted));
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
            var exists = _cropCycles.Any(cropCycle =>
                cropCycle.OrganizationId ==
                    organizationId &&
                cropCycle.LandId == landId &&
                cropCycle.LandPlotId == landPlotId &&
                cropCycle.Status !=
                    CropCycleStatus.Cancelled &&
                !cropCycle.IsDeleted &&
                (
                    excludedCropCycleId == null ||
                    cropCycle.Id !=
                        excludedCropCycleId.Value
                ) &&
                cropCycle.PlannedStartDate <=
                    expectedHarvestDate &&
                plannedStartDate <=
                    cropCycle.ExpectedHarvestDate);

            return Task.FromResult(exists);
        }

        public Task<bool> HasInProgressCycleAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default)
        {
            var exists = _cropCycles.Any(cropCycle =>
                cropCycle.OrganizationId ==
                    organizationId &&
                cropCycle.LandId == landId &&
                cropCycle.LandPlotId == landPlotId &&
                cropCycle.Status ==
                    CropCycleStatus.InProgress &&
                !cropCycle.IsDeleted &&
                (
                    excludedCropCycleId == null ||
                    cropCycle.Id !=
                        excludedCropCycleId.Value
                ));

            return Task.FromResult(exists);
        }

        public Task<bool> HasActiveCycleForLandAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            var exists = _cropCycles.Any(cropCycle =>
                cropCycle.OrganizationId ==
                    organizationId &&
                cropCycle.LandId == landId &&
                (
                    cropCycle.Status ==
                        CropCycleStatus.Planned ||
                    cropCycle.Status ==
                        CropCycleStatus.InProgress
                ) &&
                !cropCycle.IsDeleted);

            return Task.FromResult(exists);
        }

        public Task<bool> HasActiveCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            var exists = _cropCycles.Any(cropCycle =>
                cropCycle.OrganizationId ==
                    organizationId &&
                cropCycle.LandId == landId &&
                cropCycle.LandPlotId == landPlotId &&
                (
                    cropCycle.Status ==
                        CropCycleStatus.Planned ||
                    cropCycle.Status ==
                        CropCycleStatus.InProgress
                ) &&
                !cropCycle.IsDeleted);

            return Task.FromResult(exists);
        }

        public Task<bool> HasAnyCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            var exists = _cropCycles.Any(cropCycle =>
                cropCycle.OrganizationId ==
                    organizationId &&
                cropCycle.LandId == landId &&
                cropCycle.LandPlotId == landPlotId &&
                !cropCycle.IsDeleted);

            return Task.FromResult(exists);
        }

        public void Add(CropCycle cropCycle)
        {
            _cropCycles.Add(cropCycle);
        }

        private CropCycle? Find(
            Guid organizationId,
            Guid cropCycleId)
        {
            return _cropCycles.SingleOrDefault(cropCycle =>
                cropCycle.OrganizationId ==
                    organizationId &&
                cropCycle.Id == cropCycleId &&
                !cropCycle.IsDeleted);
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

        public Task<IReadOnlyList<Organization>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Organization> result =
                _organizations
                    .Where(organization =>
                        !organization.IsDeleted)
                    .ToArray();

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

        public Task<Organization?> GetByIdForUpdateAsync(
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
            return Task.FromResult(
                _organizations.Any(organization =>
                    organization.Code == code &&
                    !organization.IsDeleted));
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
                _commodities
                    .Where(commodity =>
                        commodity.OrganizationId ==
                            organizationId &&
                        !commodity.IsDeleted)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<Commodity?> GetByIdAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _commodities.SingleOrDefault(commodity =>
                    commodity.OrganizationId ==
                        organizationId &&
                    commodity.Id == commodityId &&
                    !commodity.IsDeleted));
        }

        public Task<Commodity?> GetByIdForUpdateAsync(
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
            return Task.FromResult(
                _commodities.Any(commodity =>
                    commodity.OrganizationId ==
                        organizationId &&
                    commodity.Code == code &&
                    !commodity.IsDeleted));
        }

        public void Add(Commodity commodity)
        {
            _commodities.Add(commodity);
        }
    }

    private sealed class FakeCultivationSopRepository :
        ICultivationSopRepository
    {
        private readonly List<CultivationSop> _sops;

        public FakeCultivationSopRepository(
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
                _sops
                    .Where(sop =>
                        sop.OrganizationId ==
                            organizationId &&
                        !sop.IsDeleted &&
                        (
                            commodityId == null ||
                            sop.CommodityId ==
                                commodityId.Value
                        ))
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<CultivationSop?> GetByIdAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _sops.SingleOrDefault(sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    sop.Id == cultivationSopId &&
                    !sop.IsDeleted));
        }

        public Task<CultivationSop?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);
        }

        public Task<bool> NameExistsAsync(
            Guid organizationId,
            Guid commodityId,
            string name,
            Guid? excludedCultivationSopId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _sops.Any(sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    sop.CommodityId == commodityId &&
                    sop.Name == name &&
                    !sop.IsDeleted &&
                    (
                        excludedCultivationSopId == null ||
                        sop.Id !=
                            excludedCultivationSopId.Value
                    )));
        }

        public void Add(CultivationSop cultivationSop)
        {
            _sops.Add(cultivationSop);
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
                _lands
                    .Where(land =>
                        land.OrganizationId ==
                            organizationId &&
                        !land.IsDeleted)
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
                    land.Id == landId &&
                    !land.IsDeleted));
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
            return Task.FromResult(
                _lands.Any(land =>
                    land.OrganizationId ==
                        organizationId &&
                    land.Code == code &&
                    !land.IsDeleted));
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
