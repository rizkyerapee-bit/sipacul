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

public sealed class
    CapitalContributionIdentityConsistencyTests
{
    private static readonly DateOnly PlannedStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    private static readonly DateOnly ContributionDate =
        new(2027, 1, 5);

    [Fact]
    public async Task
        Create_InvestorSameIdentityAcrossCycles_ShouldSucceed()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor);

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
                CreateRequest(
                    "CAP-NEW",
                    "INV-001",
                    "investor utama",
                    CapitalContributorRole.Investor));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.Contributions.Count);
    }

    [Fact]
    public async Task
        Create_InvestorSameCodeDifferentName_ShouldConflict()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing),
                unitOfWork)
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    "CAP-NEW",
                    "INV-001",
                    "Investor Berbeda",
                    CapitalContributorRole.Investor));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .ContributorIdentityConflictCode,
            result.Error.Code);

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task
        Create_RepeatedInvestorWithDifferentCode_ShouldSucceed()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "INV-001",
            "Investor Pertama",
            CapitalContributorRole.Investor);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    "CAP-NEW",
                    "INV-002",
                    "Investor Kedua",
                    CapitalContributorRole.Investor));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task
        Create_PartnerSameIdentityAcrossCycles_ShouldSucceed()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "MITRA-001",
            "Mitra Pengelola",
            CapitalContributorRole.Partner);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    "CAP-NEW",
                    "MITRA-001",
                    "mitra pengelola",
                    CapitalContributorRole.Partner));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task
        Create_PartnerDifferentCode_ShouldConflict()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "MITRA-001",
            "Mitra Pengelola",
            CapitalContributorRole.Partner);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    "CAP-NEW",
                    "MITRA-002",
                    "Mitra Pengelola",
                    CapitalContributorRole.Partner));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .ContributorIdentityConflictCode,
            result.Error.Code);
    }

    [Fact]
    public async Task
        Create_PartnerSameCodeDifferentName_ShouldConflict()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "MITRA-001",
            "Mitra Pengelola",
            CapitalContributorRole.Partner);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    "CAP-NEW",
                    "MITRA-001",
                    "Mitra Berbeda",
                    CapitalContributorRole.Partner));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .ContributorIdentityConflictCode,
            result.Error.Code);
    }

    [Fact]
    public async Task
        Create_IdentityInOtherOrganization_ShouldNotConflict()
    {
        var context = CreateContext();

        var otherOrganization =
            Organization.Create(
                "ORG-OTHER",
                "Organisasi Lain");

        var existing = CreateContribution(
            otherOrganization.Id,
            Guid.NewGuid(),
            "CAP-OTHER",
            "MITRA-001",
            "Mitra Organisasi Lain",
            CapitalContributorRole.Partner);

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing),
                new FakeUnitOfWork())
            .CreateAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                CreateRequest(
                    "CAP-NEW",
                    "MITRA-001",
                    "Mitra Pengelola",
                    CapitalContributorRole.Partner));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task
        Update_CurrentPartnerIdentity_ShouldExcludeCurrent()
    {
        var context = CreateContext();

        var current = CreateContribution(
            context.Organization.Id,
            context.CropCycle.Id,
            "CAP-CURRENT",
            "MITRA-001",
            "Mitra Pengelola",
            CapitalContributorRole.Partner);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    current),
                unitOfWork)
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                current.Id,
                new UpdateCapitalContributionRequest(
                    ContributionDate.AddDays(1),
                    "MITRA-001",
                    "Mitra Pengelola",
                    CapitalContributorRole.Partner,
                    3000000,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    "Modal diperbarui"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3000000m, current.Amount);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task
        Update_ToConflictingInvestorIdentity_ShouldNotMutate()
    {
        var context = CreateContext();

        var existing = CreateContribution(
            context.Organization.Id,
            context.SecondCropCycle.Id,
            "CAP-EXISTING",
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor);

        var current = CreateContribution(
            context.Organization.Id,
            context.CropCycle.Id,
            "CAP-CURRENT",
            "INV-002",
            "Investor Kedua",
            CapitalContributorRole.Investor);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                context,
                new FakeCapitalContributionRepository(
                    existing,
                    current),
                unitOfWork)
            .UpdateDraftAsync(
                context.Organization.Id,
                context.CropCycle.Id,
                current.Id,
                new UpdateCapitalContributionRequest(
                    ContributionDate.AddDays(1),
                    "INV-001",
                    "Nama Yang Salah",
                    CapitalContributorRole.Investor,
                    5000000,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.True(result.IsFailure);

        Assert.Equal(
            CapitalContributionErrors
                .ContributorIdentityConflictCode,
            result.Error.Code);

        Assert.Equal("INV-002", current.ContributorCode);

        Assert.Equal(
            "Investor Kedua",
            current.ContributorName);

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static CapitalContributionService CreateService(
        TestContext context,
        ICapitalContributionRepository repository,
        IUnitOfWork unitOfWork)
    {
        return new CapitalContributionService(
            repository,
            new FakeCropCycleRepository(
                context.CropCycle,
                context.SecondCropCycle),
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

        var firstCycle = CropCycle.Create(
            organization.Id,
            "SC-001",
            "Musim Tanam Pertama",
            commodity.Id,
            sop.Id,
            land.Id,
            plot.Id,
            5000,
            AreaUnit.SquareMeter,
            PlannedStart,
            ExpectedHarvest,
            null);

        var secondCycle = CropCycle.Create(
            organization.Id,
            "SC-002",
            "Musim Tanam Kedua",
            commodity.Id,
            sop.Id,
            land.Id,
            plot.Id,
            5000,
            AreaUnit.SquareMeter,
            PlannedStart.AddYears(1),
            ExpectedHarvest.AddYears(1),
            null);

        return new TestContext(
            organization,
            firstCycle,
            secondCycle);
    }

    private static CreateCapitalContributionRequest
        CreateRequest(
            string code,
            string contributorCode,
            string contributorName,
            CapitalContributorRole contributorRole)
    {
        return new CreateCapitalContributionRequest(
            code,
            ContributionDate,
            contributorCode,
            contributorName,
            contributorRole,
            1000000,
            CapitalContributionPaymentMethod.BankTransfer,
            null,
            null);
    }

    private static CapitalContribution
        CreateContribution(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            string contributorCode,
            string contributorName,
            CapitalContributorRole contributorRole)
    {
        return CapitalContribution.Create(
            organizationId,
            cropCycleId,
            code,
            ContributionDate,
            contributorCode,
            contributorName,
            contributorRole,
            1000000,
            CapitalContributionPaymentMethod.BankTransfer,
            null,
            null);
    }

    private sealed record TestContext(
        Organization Organization,
        CropCycle CropCycle,
        CropCycle SecondCropCycle);

    private sealed class
        FakeCapitalContributionRepository :
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
            IReadOnlyList<CapitalContribution> result =
                Contributions
                    .Where(contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.CropCycleId ==
                            cropCycleId &&
                        !contribution.IsDeleted)
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
            return Task.FromResult(
                Contributions.Any(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    contribution.Code == code &&
                    !contribution.IsDeleted));
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
                Contributions
                    .Where(contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.ContributorRole ==
                            contributorRole &&
                        contribution.ContributorCode ==
                            contributorCode &&
                        !contribution.IsDeleted &&
                        (
                            !excludedContributionId.HasValue ||
                            contribution.Id !=
                                excludedContributionId.Value
                        ))
                    .OrderBy(contribution =>
                        contribution.CreatedAt)
                    .ThenBy(contribution =>
                        contribution.Id)
                    .FirstOrDefault());
        }

        public Task<CapitalContribution?>
            GetPartnerIdentityAsync(
                Guid organizationId,
                Guid? excludedContributionId = null,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Contributions
                    .Where(contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.ContributorRole ==
                            CapitalContributorRole.Partner &&
                        !contribution.IsDeleted &&
                        (
                            !excludedContributionId.HasValue ||
                            contribution.Id !=
                                excludedContributionId.Value
                        ))
                    .OrderBy(contribution =>
                        contribution.CreatedAt)
                    .ThenBy(contribution =>
                        contribution.Id)
                    .FirstOrDefault());
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
                _cropCycles.SingleOrDefault(
                    cropCycle =>
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
        private readonly List<Organization> _organizations;

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
