using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.Expenses;
using SiPacul.Application.Finance.Expenses.Contracts;
using SiPacul.Application.Finance.Expenses.Persistence;
using SiPacul.Application.Finance.Expenses.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using Xunit;

namespace SiPacul.Application.Tests.Finance.Expenses;

public sealed class CultivationExpenseServiceTests
{
    private static readonly DateOnly PlannedStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    private static readonly DateOnly ExpenseDate =
        new(2027, 1, 5);

    [Fact]
    public async Task Create_WhenValid_ShouldCreateAndSave()
    {
        var context = CreateContext();
        var repository =
            new FakeCultivationExpenseRepository();
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
        Assert.Equal("EXP-001", result.Value.Code);
        Assert.Equal(1250000.13m, result.Value.Amount);

        Assert.Equal(
            CultivationExpenseStatus.Draft,
            result.Value.Status);

        Assert.False(result.Value.IsRecognizedCost);
        Assert.Single(repository.Expenses);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenRequestNull_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                null!);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldFail()
    {
        var context = CreateContext();

        var service = new CultivationExpenseService(
            new FakeCultivationExpenseRepository(),
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
            CultivationExpenseErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCropCycleMissing_ShouldFail()
    {
        var context = CreateContext();

        var service = new CultivationExpenseService(
            new FakeCultivationExpenseRepository(),
            new FakeCropCycleRepository(),
            new FakeOrganizationRepository(
                context.Organization),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            context.Organization.Id,
            context.CropCycle.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors
                .CropCycleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldFail()
    {
        var context = CreateContext();

        var existing = CreateExpense(context);

        var repository =
            new FakeCultivationExpenseRepository(
                existing);

        var result = await CreateService(
                context,
                repository,
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors
                .CodeAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDateTooEarly_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    ExpenseDate =
                        PlannedStart.AddYears(-1)
                            .AddDays(-1)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.DateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDateTooLate_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    ExpenseDate =
                        ExpectedHarvest.AddYears(1)
                            .AddDays(1)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.DateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WithInvalidCategory_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    Category =
                        (CultivationExpenseCategory)999
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldPassNormalizedFilters()
    {
        var context = CreateContext();

        var repository =
            new FakeCultivationExpenseRepository(
                CreateExpense(context));

        var filter = new CultivationExpenseFilter(
            CultivationExpenseStatus.Draft,
            CultivationExpenseCategory.LandLease,
            ExpenseDate.AddDays(-1),
            ExpenseDate.AddDays(1),
            "  Pemilik  ");

        var result = await CreateService(
                context,
                repository,
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                filter);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CultivationExpenseStatus.Draft,
            repository.LastStatus);

        Assert.Equal(
            CultivationExpenseCategory.LandLease,
            repository.LastCategory);

        Assert.Equal("Pemilik", repository.LastPayeeName);
    }

    [Fact]
    public async Task GetAll_WhenDateRangeInvalid_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CultivationExpenseFilter(
                    ExpenseDateFrom:
                        ExpenseDate.AddDays(1),
                    ExpenseDateTo:
                        ExpenseDate));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithUnsupportedStatus_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CultivationExpenseFilter(
                    Status:
                        (CultivationExpenseStatus)999));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldMapResponse()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(expense.Id, result.Value.Id);
        Assert.Equal(expense.Amount, result.Value.Amount);
        Assert.False(result.Value.IsRecognizedCost);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WhenValid_ShouldUpdateAndSave()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                unitOfWork)
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id,
                new UpdateCultivationExpenseRequest(
                    ExpenseDate.AddDays(1),
                    CultivationExpenseCategory.Transport,
                    "  Transport panen  ",
                    500000.125m,
                    "  Koperasi Angkut  ",
                    "  REF-2  ",
                    null,
                    "  Tunai  "));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CultivationExpenseCategory.Transport,
            result.Value.Category);

        Assert.Equal(
            "Transport panen",
            result.Value.Description);

        Assert.Equal(500000.13m, result.Value.Amount);
        Assert.Equal("Koperasi Angkut", result.Value.PayeeName);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WhenConfirmed_ShouldFail()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        expense.Confirm();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id,
                UpdateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WhenDateOutOfRange_ShouldFail()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id,
                UpdateRequest() with
                {
                    ExpenseDate =
                        ExpectedHarvest.AddYears(2)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.DateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Confirm_WhenDraft_ShouldRecognizeAndSave()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                unitOfWork)
            .ConfirmAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CultivationExpenseStatus.Confirmed,
            result.Value.Status);

        Assert.True(result.Value.IsRecognizedCost);
        Assert.NotNull(result.Value.ConfirmedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Confirm_WhenAlreadyConfirmed_ShouldFail()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        expense.Confirm();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .ConfirmAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromDraft_ShouldCancelAndSave()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                unitOfWork)
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id,
                new CancelCultivationExpenseRequest(
                    "  Biaya dibatalkan  "));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CultivationExpenseStatus.Cancelled,
            result.Value.Status);

        Assert.Equal(
            "Biaya dibatalkan",
            result.Value.CancellationReason);

        Assert.False(result.Value.IsRecognizedCost);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cancel_FromConfirmed_ShouldPreserveConfirmedAt()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        expense.Confirm();
        var confirmedAt = expense.ConfirmedAt;

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id,
                new CancelCultivationExpenseRequest(
                    "Bukti dikoreksi"));

        Assert.True(result.IsSuccess);
        Assert.Equal(confirmedAt, result.Value.ConfirmedAt);
        Assert.False(result.Value.IsRecognizedCost);
    }

    [Fact]
    public async Task Cancel_WithBlankReason_ShouldFail()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id,
                new CancelCultivationExpenseRequest(" "));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task CrossOrganizationCropCycle_ShouldFail()
    {
        var context = CreateContext();
        var otherOrganization =
            Organization.Create(
                "ORG-002",
                "Organisasi Lain");

        var service = new CultivationExpenseService(
            new FakeCultivationExpenseRepository(),
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization,
                otherOrganization),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            otherOrganization.Id,
            context.CropCycle.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors
                .CropCycleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task EmptyOrganizationId_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                Guid.Empty,
                context.CropCycle.Id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task EmptyCropCycleId_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                Guid.Empty);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task EmptyExpenseId_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                Guid.Empty);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CultivationExpenseErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task ConfirmedResponse_ShouldExposeRecognizedCost()
    {
        var context = CreateContext();
        var expense = CreateExpense(context);
        expense.Confirm();

        var result = await CreateService(
                context,
                new FakeCultivationExpenseRepository(
                    expense),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                expense.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsRecognizedCost);
    }

    private static CultivationExpenseService CreateService(
        TestContext context,
        ICultivationExpenseRepository repository,
        IUnitOfWork unitOfWork)
    {
        return new CultivationExpenseService(
            repository,
            new FakeCropCycleRepository(
                context.CropCycle),
            new FakeOrganizationRepository(
                context.Organization),
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
            PlannedStart,
            ExpectedHarvest,
            null);

        return new TestContext(
            organization,
            cropCycle);
    }

    private static CreateCultivationExpenseRequest
        CreateRequest()
    {
        return new CreateCultivationExpenseRequest(
            "  exp-001  ",
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "  Sewa lahan  ",
            1250000.125m,
            "  Pemilik Lahan  ",
            "  REF-001  ",
            "  https://example.test/ref-001  ",
            "  Transfer  ");
    }

    private static UpdateCultivationExpenseRequest
        UpdateRequest()
    {
        return new UpdateCultivationExpenseRequest(
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "Sewa lahan",
            1250000,
            "Pemilik Lahan",
            "REF-001",
            "https://example.test/ref-001",
            "Transfer");
    }

    private static CultivationExpense CreateExpense(
        TestContext context,
        string code = "EXP-001")
    {
        return CultivationExpense.Create(
            context.Organization.Id,
            context.CropCycle.Id,
            code,
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "Sewa lahan",
            1250000,
            "Pemilik Lahan",
            "REF-001",
            "https://example.test/ref-001",
            "Transfer");
    }

    private sealed record TestContext(
        Organization Organization,
        CropCycle CropCycle);

    private sealed class FakeCultivationExpenseRepository :
        ICultivationExpenseRepository
    {
        public FakeCultivationExpenseRepository(
            params CultivationExpense[] expenses)
        {
            Expenses = expenses.ToList();
        }

        public List<CultivationExpense> Expenses
        {
            get;
        }

        public CultivationExpenseStatus? LastStatus
        {
            get;
            private set;
        }

        public CultivationExpenseCategory? LastCategory
        {
            get;
            private set;
        }

        public string? LastPayeeName
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<CultivationExpense>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                CultivationExpenseStatus? status = null,
                CultivationExpenseCategory? category = null,
                DateOnly? expenseDateFrom = null,
                DateOnly? expenseDateTo = null,
                string? payeeName = null,
                CancellationToken cancellationToken = default)
        {
            LastStatus = status;
            LastCategory = category;
            LastPayeeName = payeeName;

            IEnumerable<CultivationExpense> query =
                Expenses.Where(expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    !expense.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(expense =>
                    expense.Status == status.Value);
            }

            if (category.HasValue)
            {
                query = query.Where(expense =>
                    expense.Category == category.Value);
            }

            if (expenseDateFrom.HasValue)
            {
                query = query.Where(expense =>
                    expense.ExpenseDate >=
                        expenseDateFrom.Value);
            }

            if (expenseDateTo.HasValue)
            {
                query = query.Where(expense =>
                    expense.ExpenseDate <=
                        expenseDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(payeeName))
            {
                query = query.Where(expense =>
                    expense.PayeeName?.Contains(
                        payeeName,
                        StringComparison.OrdinalIgnoreCase) ==
                    true);
            }

            IReadOnlyList<CultivationExpense> result =
                query
                    .OrderBy(expense =>
                        expense.ExpenseDate)
                    .ThenBy(expense => expense.Code)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<CultivationExpense?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    expenseId));
        }

        public Task<CultivationExpense?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid expenseId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    expenseId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Expenses.Any(expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    expense.Code == code &&
                    !expense.IsDeleted));
        }

        public void Add(CultivationExpense expense)
        {
            Expenses.Add(expense);
        }

        private CultivationExpense? Find(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId)
        {
            return Expenses.SingleOrDefault(expense =>
                expense.OrganizationId ==
                    organizationId &&
                expense.CropCycleId ==
                    cropCycleId &&
                expense.Id == expenseId &&
                !expense.IsDeleted);
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
                _cropCycles
                    .Where(cropCycle =>
                        cropCycle.OrganizationId ==
                            organizationId &&
                        !cropCycle.IsDeleted)
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
            return Task.FromResult(false);
        }

        public void Add(Organization organization)
        {
            _organizations.Add(organization);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
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
