using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.CapitalContributions;
using SiPacul.Application.Finance.CapitalContributions.Contracts;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.CapitalContributions.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using Xunit;

namespace SiPacul.Application.Tests.Finance.CapitalContributions;

public sealed class CapitalContributionServiceTests
{
    private static readonly DateOnly PlannedStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    private static readonly DateOnly ContributionDate =
        new(2027, 1, 5);

    [Fact]
    public async Task Create_WhenValid_ShouldCreateAndSave()
    {
        var context = CreateContext();

        var repository =
            new FakeCapitalContributionRepository();

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
        Assert.Equal("CAP-001", result.Value.Code);

        Assert.Equal(
            "INV-001",
            result.Value.ContributorCode);

        Assert.Equal(
            "Investor Utama",
            result.Value.ContributorName);

        Assert.Equal(
            CapitalContributorRole.Investor,
            result.Value.ContributorRole);

        Assert.Equal(
            10000000.13m,
            result.Value.Amount);

        Assert.Equal(
            CapitalContributionStatus.Draft,
            result.Value.Status);

        Assert.False(result.Value.IsConfirmedCapital);
        Assert.True(result.Value.IsInvestorCapital);
        Assert.False(result.Value.IsPartnerCapital);
        Assert.Single(repository.Contributions);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenRequestNull_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                null!);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldFail()
    {
        var context = CreateContext();

        var service = new CapitalContributionService(
            new FakeCapitalContributionRepository(),
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
            CapitalContributionErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCropCycleMissing_ShouldFail()
    {
        var context = CreateContext();

        var service = new CapitalContributionService(
            new FakeCapitalContributionRepository(),
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
            CapitalContributionErrors
                .CropCycleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldFail()
    {
        var context = CreateContext();

        var existing =
            CreateContribution(context);

        var repository =
            new FakeCapitalContributionRepository(
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
            CapitalContributionErrors
                .CodeAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDateTooEarly_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    ContributionDate =
                        PlannedStart.AddYears(-1)
                            .AddDays(-1)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .DateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDateTooLate_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    ContributionDate =
                        ExpectedHarvest.AddYears(1)
                            .AddDays(1)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .DateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDomainValidationFails_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest() with
                {
                    Amount = 0
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldPassNormalizedFilters()
    {
        var context = CreateContext();

        var investor =
            CreateContribution(context);

        investor.Confirm();

        var partner =
            CapitalContribution.Create(
                context.Organization.Id,
                context.CropCycle.Id,
                "CAP-002",
                ContributionDate.AddDays(1),
                "MITRA-001",
                "Mitra Pengelola",
                CapitalContributorRole.Partner,
                2500000,
                CapitalContributionPaymentMethod.Cash,
                null,
                null);

        var repository =
            new FakeCapitalContributionRepository(
                investor,
                partner);

        var result = await CreateService(
                context,
                repository,
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CapitalContributionFilter(
                    CapitalContributionStatus.Confirmed,
                    CapitalContributorRole.Investor,
                    new DateOnly(2027, 1, 1),
                    new DateOnly(2027, 1, 31),
                    "  inv-001  ",
                    "  investor  "));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        Assert.Equal(
            CapitalContributionStatus.Confirmed,
            repository.LastStatus);

        Assert.Equal(
            CapitalContributorRole.Investor,
            repository.LastContributorRole);

        Assert.Equal(
            "INV-001",
            repository.LastContributorCode);

        Assert.Equal(
            "investor",
            repository.LastContributorName);
    }

    [Fact]
    public async Task GetAll_WithInvalidStatus_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CapitalContributionFilter(
                    (CapitalContributionStatus)999));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithInvalidRole_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CapitalContributionFilter(
                    ContributorRole:
                        (CapitalContributorRole)999));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithInvalidDateRange_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CapitalContributionFilter(
                    ContributionDateFrom:
                        new DateOnly(2027, 2, 1),
                    ContributionDateTo:
                        new DateOnly(2027, 1, 1)));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithBlankContributorCode_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                new CapitalContributionFilter(
                    ContributorCode: " "));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnResponse()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            contribution.Id,
            result.Value.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldFail()
    {
        var context = CreateContext();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WhenValid_ShouldUpdateAndSave()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                unitOfWork)
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                UpdateRequest());

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "MITRA-001",
            result.Value.ContributorCode);

        Assert.Equal(
            CapitalContributorRole.Partner,
            result.Value.ContributorRole);

        Assert.Equal(
            2500000.13m,
            result.Value.Amount);

        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WithSameValues_ShouldNotSave()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                unitOfWork)
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                new UpdateCapitalContributionRequest(
                    ContributionDate,
                    "INV-001",
                    "Investor Utama",
                    CapitalContributorRole.Investor,
                    10000000.125m,
                    CapitalContributionPaymentMethod
                        .BankTransfer,
                    "TRF-001",
                    "Modal tahap pertama"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WhenConfirmed_ShouldFail()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        contribution.Confirm();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                UpdateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WhenDateOutOfRange_ShouldFail()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                UpdateRequest() with
                {
                    ContributionDate =
                        ExpectedHarvest.AddYears(1)
                            .AddDays(1)
                });

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .DateOutOfRangeCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Confirm_WhenDraft_ShouldConfirmAndSave()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                unitOfWork)
            .ConfirmAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CapitalContributionStatus.Confirmed,
            result.Value.Status);

        Assert.True(result.Value.IsConfirmedCapital);
        Assert.NotNull(result.Value.ConfirmedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Confirm_WhenAlreadyConfirmed_ShouldFail()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        contribution.Confirm();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .ConfirmAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromConfirmed_ShouldPreserveConfirmedAt()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        contribution.Confirm();

        var confirmedAt =
            contribution.ConfirmedAt;

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                unitOfWork)
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                new CancelCapitalContributionRequest(
                    "  Kontribusi dikoreksi  "));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            CapitalContributionStatus.Cancelled,
            result.Value.Status);

        Assert.False(result.Value.IsConfirmedCapital);

        Assert.Equal(
            confirmedAt,
            result.Value.ConfirmedAt);

        Assert.Equal(
            "Kontribusi dikoreksi",
            result.Value.CancellationReason);

        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cancel_WithBlankReason_ShouldFail()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                new CancelCapitalContributionRequest(" "));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_WhenAlreadyCancelled_ShouldFail()
    {
        var context = CreateContext();

        var contribution =
            CreateContribution(context);

        contribution.Cancel("Batal");

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .CancelAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id,
                new CancelCapitalContributionRequest(
                    "Batal lagi"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .InvalidStatusTransitionCode,
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

        var service = new CapitalContributionService(
            new FakeCapitalContributionRepository(),
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
            CapitalContributionErrors
                .CropCycleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task EmptyIdentifiers_ShouldFail()
    {
        var context = CreateContext();

        var service = CreateService(
            context,
            new FakeCapitalContributionRepository(),
            new FakeUnitOfWork());

        var emptyOrganizationResult =
            await service.GetAllAsync(
                Guid.Empty,
                context.CropCycle.Id);

        var emptyCropCycleResult =
            await service.GetAllAsync(
                context.Organization.Id,
                Guid.Empty);

        var emptyContributionResult =
            await service.GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                Guid.Empty);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            emptyOrganizationResult.Error.Code);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            emptyCropCycleResult.Error.Code);

        Assert.Equal(
            CapitalContributionErrors.ValidationCode,
            emptyContributionResult.Error.Code);
    }

    [Fact]
    public async Task PartnerResponse_ShouldExposePartnerFlag()
    {
        var context = CreateContext();

        var contribution =
            CapitalContribution.Create(
                context.Organization.Id,
                context.CropCycle.Id,
                "CAP-PARTNER",
                ContributionDate,
                "MITRA-001",
                "Mitra Pengelola",
                CapitalContributorRole.Partner,
                2500000,
                CapitalContributionPaymentMethod.Cash,
                null,
                null);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    contribution),
                new FakeUnitOfWork())
            .GetByIdAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                contribution.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPartnerCapital);
        Assert.False(result.Value.IsInvestorCapital);
    }

    private static CapitalContributionService CreateService(
        TestContext context,
        ICapitalContributionRepository repository,
        IUnitOfWork unitOfWork)
    {
        return new CapitalContributionService(
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

    private static CreateCapitalContributionRequest
        CreateRequest()
    {
        return new CreateCapitalContributionRequest(
            "  cap-001  ",
            ContributionDate,
            "  inv-001  ",
            "  Investor Utama  ",
            CapitalContributorRole.Investor,
            10000000.125m,
            CapitalContributionPaymentMethod.BankTransfer,
            "  TRF-001  ",
            "  Modal tahap pertama  ");
    }

    private static UpdateCapitalContributionRequest
        UpdateRequest()
    {
        return new UpdateCapitalContributionRequest(
            ContributionDate.AddDays(1),
            "  mitra-001  ",
            "  Mitra Pengelola  ",
            CapitalContributorRole.Partner,
            2500000.125m,
            CapitalContributionPaymentMethod.Cash,
            "  CASH-002  ",
            "  Modal Mitra  ");
    }

    private static CapitalContribution
        CreateContribution(
            TestContext context,
            string code = "CAP-001")
    {
        return CapitalContribution.Create(
            context.Organization.Id,
            context.CropCycle.Id,
            code,
            ContributionDate,
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor,
            10000000.125m,
            CapitalContributionPaymentMethod.BankTransfer,
            "TRF-001",
            "Modal tahap pertama");
    }

    private sealed record TestContext(
        Organization Organization,
        CropCycle CropCycle);

    private sealed class FakeCapitalContributionRepository :
        ICapitalContributionRepository
    {
        public FakeCapitalContributionRepository(
            params CapitalContribution[] contributions)
        {
            Contributions = contributions.ToList();
        }

        public List<CapitalContribution> Contributions
        {
            get;
        }

        public CapitalContributionStatus? LastStatus
        {
            get;
            private set;
        }

        public CapitalContributorRole? LastContributorRole
        {
            get;
            private set;
        }

        public string? LastContributorCode
        {
            get;
            private set;
        }

        public string? LastContributorName
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<CapitalContribution>>
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
            LastStatus = status;
            LastContributorRole = contributorRole;
            LastContributorCode = contributorCode;
            LastContributorName = contributorName;

            IEnumerable<CapitalContribution> query =
                Contributions.Where(contribution =>
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

            if (contributionDateFrom.HasValue)
            {
                query = query.Where(contribution =>
                    contribution.ContributionDate >=
                        contributionDateFrom.Value);
            }

            if (contributionDateTo.HasValue)
            {
                query = query.Where(contribution =>
                    contribution.ContributionDate <=
                        contributionDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    contributorCode))
            {
                query = query.Where(contribution =>
                    contribution.ContributorCode ==
                        contributorCode);
            }

            if (!string.IsNullOrWhiteSpace(
                    contributorName))
            {
                query = query.Where(contribution =>
                    contribution.ContributorName.Contains(
                        contributorName,
                        StringComparison.OrdinalIgnoreCase));
            }

            IReadOnlyList<CapitalContribution> result =
                query
                    .OrderBy(contribution =>
                        contribution.ContributionDate)
                    .ThenBy(contribution =>
                        contribution.Code)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<CapitalContribution?> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    contributionId));
        }

        public Task<CapitalContribution?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid contributionId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    cropCycleId,
                    contributionId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Contributions.Any(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    contribution.Code == code &&
                    !contribution.IsDeleted));
        }

        public void Add(CapitalContribution contribution)
        {
            Contributions.Add(contribution);
        }

        private CapitalContribution? Find(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId)
        {
            return Contributions.SingleOrDefault(
                contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    contribution.Id ==
                        contributionId &&
                    !contribution.IsDeleted);
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
            _cropCycles.Add(cropCycle);
        }

        private CropCycle? Find(
            Guid organizationId,
            Guid cropCycleId)
        {
            return _cropCycles.SingleOrDefault(
                cropCycle =>
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
        public int SaveCount
        {
            get;
            private set;
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }
}
