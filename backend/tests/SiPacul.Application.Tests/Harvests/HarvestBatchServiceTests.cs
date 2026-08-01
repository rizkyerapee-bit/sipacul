using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Services;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.Harvests;
using SiPacul.Application.Harvests.Contracts;
using SiPacul.Application.Harvests.Persistence;
using SiPacul.Application.Harvests.Services;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Harvests;

public sealed class HarvestBatchServiceTests
{
    private static readonly DateOnly CycleStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    private static readonly DateOnly HarvestDate =
        new(2027, 5, 1);

    [Fact]
    public async Task Create_WhenValid_ShouldCreateAndSave()
    {
        var context = CreateContext(startCycle: true);
        var repository =
            new FakeHarvestBatchRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                repository,
                unitOfWork)
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("HRV-001", result.Value.Code);
        Assert.Equal(975m, result.Value.NetQuantity);

        Assert.Equal(
            HarvestBatchStatus.Draft,
            result.Value.Status);

        Assert.Equal(0m, result.Value.AvailableQuantity);
        Assert.Single(repository.Batches);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldFail()
    {
        var context = CreateContext(startCycle: true);

        var service = new HarvestBatchService(
            new FakeHarvestBatchRepository(),
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCropCyclePlanned_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .CropCycleNotInProgressCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDateBeforeActualStart_ShouldFail()
    {
        var context = CreateContext(startCycle: true);

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    HarvestDate =
                        CycleStart.AddDays(-1)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .InvalidHarvestDateCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldFail()
    {
        var context = CreateContext(startCycle: true);

        var existing = CreateBatch(context);

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(
                    existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .CodeAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithFilter_ShouldReturnMatchingBatch()
    {
        var context = CreateContext(startCycle: true);

        var first = CreateBatch(context);
        var second = CreateBatch(
            context,
            "HRV-002",
            HarvestDate.AddDays(2),
            HarvestQuantityUnit.Quintal,
            "Grade B");

        second.Confirm();

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(
                    first,
                    second),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new HarvestBatchFilter(
                    HarvestBatchStatus.Confirmed,
                    HarvestDate.AddDays(1),
                    HarvestDate.AddDays(3),
                    HarvestQuantityUnit.Quintal,
                    "grade b"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("HRV-002", result.Value[0].Code);
        Assert.Equal(975m, result.Value[0].AvailableQuantity);
    }

    [Fact]
    public async Task GetAll_WhenDateRangeInvalid_ShouldFail()
    {
        var context = CreateContext(startCycle: true);

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new HarvestBatchFilter(
                    HarvestDateFrom:
                        HarvestDate.AddDays(1),
                    HarvestDateTo:
                        HarvestDate));

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetById_FromAnotherOrganization_ShouldFail()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(batch),
                new FakeUnitOfWork())
            .GetByIdAsync(
                Guid.NewGuid(),
                context.CropCycle.Id,
                batch.Id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WhenValid_ShouldUpdateAndSave()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(batch),
                unitOfWork)
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                batch.Id,
                new UpdateHarvestBatchRequest(
                    HarvestDate.AddDays(1),
                    1200,
                    100,
                    HarvestQuantityUnit.Kilogram,
                    "Grade B",
                    "Gudang Timur",
                    "Panen kedua"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1100m, result.Value.NetQuantity);
        Assert.Equal("Grade B", result.Value.QualityGrade);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WhenConfirmed_ShouldFail()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        batch.Confirm();

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(batch),
                new FakeUnitOfWork())
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                batch.Id,
                new UpdateHarvestBatchRequest(
                    HarvestDate,
                    1000,
                    20,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Confirm_WhenValid_ShouldConfirmAndExposeStock()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(batch),
                unitOfWork)
            .ConfirmAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                batch.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            HarvestBatchStatus.Confirmed,
            result.Value.Status);

        Assert.Equal(975m, result.Value.AvailableQuantity);
        Assert.NotNull(result.Value.ConfirmedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Confirm_WhenCropCycleCompleted_ShouldFail()
    {
        var context = CreateContext(startCycle: true);
        context.CropCycle.Complete(ExpectedHarvest);

        var batch = CreateBatch(context);

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(batch),
                new FakeUnitOfWork())
            .ConfirmAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                batch.Id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .CropCycleNotInProgressCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromDraft_ShouldCancelAndSave()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(batch),
                unitOfWork)
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                batch.Id,
                new CancelHarvestBatchRequest(
                    "Data salah"));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            HarvestBatchStatus.Cancelled,
            result.Value.Status);

        Assert.Equal("Data salah", result.Value.CancellationReason);
        Assert.Equal(0m, result.Value.AvailableQuantity);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CompleteCropCycle_WhenDraftHarvestExists_ShouldFail()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);

        var service = CreateCropCycleService(
            context,
            new FakeHarvestBatchRepository(batch));

        var result = await service.CompleteAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            new CompleteCropCycleRequest(
                ExpectedHarvest));

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .CropCycleHasDraftHarvestsCode,
            result.Error.Code);

        Assert.Equal(
            CropCycleStatus.InProgress,
            context.CropCycle.Status);
    }

    [Fact]
    public async Task CompleteCropCycle_WithConfirmedHarvest_ShouldSucceed()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        batch.Confirm();

        var service = CreateCropCycleService(
            context,
            new FakeHarvestBatchRepository(batch));

        var result = await service.CompleteAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            new CompleteCropCycleRequest(
                ExpectedHarvest));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CropCycleStatus.Completed,
            context.CropCycle.Status);
    }

    [Fact]
    public async Task CancelCropCycle_WhenConfirmedHarvestExists_ShouldFail()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        batch.Confirm();

        var service = CreateCropCycleService(
            context,
            new FakeHarvestBatchRepository(batch));

        var result = await service.CancelAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            new CancelCropCycleRequest(
                "Musim dihentikan"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors
                .CropCycleHasNonCancelledHarvestsCode,
            result.Error.Code);

        Assert.Equal(
            CropCycleStatus.InProgress,
            context.CropCycle.Status);
    }

    [Fact]
    public async Task CancelCropCycle_AfterHarvestCancelled_ShouldSucceed()
    {
        var context = CreateContext(startCycle: true);
        var batch = CreateBatch(context);
        batch.Cancel("Panen dibatalkan");

        var service = CreateCropCycleService(
            context,
            new FakeHarvestBatchRepository(batch));

        var result = await service.CancelAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            new CancelCropCycleRequest(
                "Musim dihentikan"));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CropCycleStatus.Cancelled,
            context.CropCycle.Status);
    }

    [Fact]
    public async Task GetById_WhenBatchMissing_ShouldFail()
    {
        var context = CreateContext(startCycle: true);

        var result = await CreateService(
                context,
                new FakeHarvestBatchRepository(),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Equal(
            HarvestBatchErrors.NotFoundCode,
            result.Error.Code);
    }

    private static HarvestBatchService CreateService(
        TestContext context,
        IHarvestBatchRepository repository,
        IUnitOfWork unitOfWork)
    {
        return new HarvestBatchService(
            repository,
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization),
            unitOfWork);
    }

    private static CropCycleService
        CreateCropCycleService(
            TestContext context,
            IHarvestBatchRepository repository)
    {
        return new CropCycleService(
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeCommodityRepository(
                context.Commodity),
            new FakeSopRepository(
                context.Sop),
            new FakeLandRepository(
                context.Land),
            new FakeUnitOfWork(),
            cultivationActivityRepository: null,
            harvestBatchRepository: repository);
    }

    private static TestContext CreateContext(
        bool startCycle = false)
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
            ExpectedHarvest,
            null);

        if (startCycle)
        {
            cropCycle.Start(CycleStart);
        }

        return new TestContext(
            organization,
            commodity,
            sop,
            land,
            plot,
            cropCycle);
    }

    private static CreateHarvestBatchRequest
        CreateRequest()
    {
        return new CreateHarvestBatchRequest(
            "  hrv-001  ",
            HarvestDate,
            1000,
            25,
            HarvestQuantityUnit.Kilogram,
            "Grade A",
            "Gudang Utama",
            "Panen pagi");
    }

    private static HarvestBatch CreateBatch(
        TestContext context,
        string code = "HRV-001",
        DateOnly? harvestDate = null,
        HarvestQuantityUnit unit =
            HarvestQuantityUnit.Kilogram,
        string? qualityGrade = "Grade A")
    {
        return HarvestBatch.Create(
            context.Organization.Id,
            context.CropCycle.Id,
            code,
            harvestDate ?? HarvestDate,
            1000,
            25,
            unit,
            qualityGrade,
            "Gudang Utama",
            null);
    }

    private sealed record TestContext(
        Organization Organization,
        Commodity Commodity,
        CultivationSop Sop,
        Land Land,
        LandPlot Plot,
        CropCycle CropCycle);

    private sealed class FakeHarvestBatchRepository :
        IHarvestBatchRepository
    {
        private readonly List<HarvestBatch> _batches;

        public FakeHarvestBatchRepository(
            params HarvestBatch[] batches)
        {
            _batches = batches.ToList();
        }

        public IReadOnlyList<HarvestBatch> Batches =>
            _batches;

        public Task<IReadOnlyList<HarvestBatch>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                HarvestBatchStatus? status = null,
                DateOnly? harvestDateFrom = null,
                DateOnly? harvestDateTo = null,
                HarvestQuantityUnit? quantityUnit = null,
                string? qualityGrade = null,
                CancellationToken cancellationToken = default)
        {
            IEnumerable<HarvestBatch> query =
                _batches.Where(batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    !batch.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(batch =>
                    batch.Status == status.Value);
            }

            if (harvestDateFrom.HasValue)
            {
                query = query.Where(batch =>
                    batch.HarvestDate >=
                        harvestDateFrom.Value);
            }

            if (harvestDateTo.HasValue)
            {
                query = query.Where(batch =>
                    batch.HarvestDate <=
                        harvestDateTo.Value);
            }

            if (quantityUnit.HasValue)
            {
                query = query.Where(batch =>
                    batch.QuantityUnit ==
                        quantityUnit.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    qualityGrade))
            {
                query = query.Where(batch =>
                    batch.QualityGrade?.Contains(
                        qualityGrade,
                        StringComparison.OrdinalIgnoreCase) ==
                    true);
            }

            IReadOnlyList<HarvestBatch> result =
                query
                    .OrderBy(batch => batch.HarvestDate)
                    .ThenBy(batch => batch.Code)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<HarvestBatch?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    harvestBatchId));
        }

        public Task<HarvestBatch?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    harvestBatchId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _batches.Any(batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Code == code &&
                    !batch.IsDeleted));
        }

        public Task<bool> HasDraftBatchesAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _batches.Any(batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Status ==
                        HarvestBatchStatus.Draft &&
                    !batch.IsDeleted));
        }

        public Task<bool>
            HasNonCancelledBatchesAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _batches.Any(batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Status !=
                        HarvestBatchStatus.Cancelled &&
                    !batch.IsDeleted));
        }

        public void Add(HarvestBatch harvestBatch)
        {
            _batches.Add(harvestBatch);
        }

        private HarvestBatch? Find(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId)
        {
            return _batches.SingleOrDefault(batch =>
                batch.OrganizationId ==
                    organizationId &&
                batch.CropCycleId ==
                    cropCycleId &&
                batch.Id == harvestBatchId &&
                !batch.IsDeleted);
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
                Find(
                    organizationId,
                    cropCycleId));
        }

        public Task<CropCycle?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId));
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
            _organizations =
                organizations.ToList();
        }

        public Task<IReadOnlyList<Organization>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<Organization>>(
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

    private sealed class FakeCommodityRepository :
        ICommodityRepository
    {
        private readonly List<Commodity> _commodities;

        public FakeCommodityRepository(
            params Commodity[] commodities)
        {
            _commodities = commodities.ToList();
        }

        public Task<IReadOnlyList<Commodity>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Commodity> result =
                _commodities.Where(commodity =>
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
                Find(
                    organizationId,
                    commodityId));
        }

        public Task<Commodity?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid commodityId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    commodityId));
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

        private Commodity? Find(
            Guid organizationId,
            Guid commodityId)
        {
            return _commodities.SingleOrDefault(
                commodity =>
                    commodity.OrganizationId ==
                        organizationId &&
                    commodity.Id == commodityId &&
                    !commodity.IsDeleted);
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
                Find(
                    organizationId,
                    landId));
        }

        public Task<Land?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    landId));
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

        private Land? Find(
            Guid organizationId,
            Guid landId)
        {
            return _lands.SingleOrDefault(land =>
                land.OrganizationId ==
                    organizationId &&
                land.Id == landId &&
                !land.IsDeleted);
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
