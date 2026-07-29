using SiPacul.Application.Common.Persistence;
using SiPacul.Application.MasterData.Commodities;
using SiPacul.Application.MasterData.Commodities.Contracts;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.MasterData.Commodities.Services;
using SiPacul.Application.MasterData.CommodityCategories.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.MasterData.Commodities;

public sealed class CommodityServiceTests
{
    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateCommodity()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var commodityRepository =
            new FakeCommodityRepository();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            commodityRepository,
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var request = new CreateCommodityRequest(
            "  padi  ",
            "  Padi Sawah  ",
            category.Id,
            "  Oryza sativa  ",
            "  Tanaman pangan utama  ");

        var result = await service.CreateAsync(
            organization.Id,
            request);

        Assert.True(result.IsSuccess);
        Assert.Equal("PADI", result.Value.Code);
        Assert.Equal(
            "Padi Sawah",
            result.Value.Name);
        Assert.Equal(
            category.Id,
            result.Value.CommodityCategoryId);
        Assert.Equal(
            "Oryza sativa",
            result.Value.ScientificName);
        Assert.Equal(
            "Tanaman pangan utama",
            result.Value.Description);
        Assert.Single(
            commodityRepository.Commodities);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var organizationId = Guid.NewGuid();

        var category = CreateCategory(
            organizationId);

        var service = CreateService(
            new FakeCommodityRepository(),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            organizationId,
            new CreateCommodityRequest(
                "PADI",
                "Padi",
                category.Id,
                null,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityErrors.OrganizationNotFoundCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);
    }

    [Fact]
    public async Task Create_WhenCategoryMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var service = CreateService(
            new FakeCommodityRepository(),
            new FakeCommodityCategoryRepository(),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCommodityRequest(
                "PADI",
                "Padi",
                Guid.NewGuid(),
                null,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityErrors.CategoryNotFoundCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ShouldReturnConflict()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var existingCommodity = CreateCommodity(
            organization.Id,
            category.Id,
            "PADI",
            "Padi");

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(
                existingCommodity),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCommodityRequest(
                "padi",
                "Padi Organik",
                category.Id,
                null,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityErrors.CodeAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithInvalidCode_ShouldReturnValidation()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCommodityRequest(
                "   ",
                "Padi",
                category.Id,
                null,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyOrganizationCommoditiesOrderedByName()
    {
        var organization = CreateOrganization();

        var otherOrganization = CreateOrganization(
            "ORG-002",
            "Organisasi Lain");

        var category = CreateCategory(
            organization.Id);

        var otherCategory = CreateCategory(
            otherOrganization.Id,
            "Kategori Lain");

        var repository = new FakeCommodityRepository(
            CreateCommodity(
                organization.Id,
                category.Id,
                "TOMAT",
                "Tomat"),
            CreateCommodity(
                organization.Id,
                category.Id,
                "CABAI",
                "Cabai"),
            CreateCommodity(
                otherOrganization.Id,
                otherCategory.Id,
                "PADI",
                "Padi"));

        var service = CreateService(
            repository,
            new FakeCommodityCategoryRepository(
                category,
                otherCategory),
            new FakeOrganizationRepository(
                organization,
                otherOrganization),
            new FakeUnitOfWork());

        var result = await service.GetAllAsync(
            organization.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(
            "Cabai",
            result.Value[0].Name);
        Assert.Equal(
            "Tomat",
            result.Value[1].Name);
    }

    [Fact]
    public async Task GetById_WhenCommodityExists_ShouldReturnCommodity()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var commodity = CreateCommodity(
            organization.Id,
            category.Id);

        var service = CreateService(
            new FakeCommodityRepository(
                commodity),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            commodity.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            commodity.Id,
            result.Value.Id);
    }

    [Fact]
    public async Task GetById_WhenCommodityMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var service = CreateService(
            new FakeCommodityRepository(),
            new FakeCommodityCategoryRepository(),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Update_WithValidRequest_ShouldUpdateAndSave()
    {
        var organization = CreateOrganization();

        var firstCategory = CreateCategory(
            organization.Id,
            "Tanaman Pangan");

        var secondCategory = CreateCategory(
            organization.Id,
            "Hortikultura");

        var commodity = CreateCommodity(
            organization.Id,
            firstCategory.Id);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(
                commodity),
            new FakeCommodityCategoryRepository(
                firstCategory,
                secondCategory),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            commodity.Id,
            new UpdateCommodityRequest(
                "  Padi Organik  ",
                secondCategory.Id,
                "  Oryza sativa  ",
                "  Padi budidaya organik  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Padi Organik",
            result.Value.Name);
        Assert.Equal(
            secondCategory.Id,
            result.Value.CommodityCategoryId);
        Assert.Equal(
            "Oryza sativa",
            result.Value.ScientificName);
        Assert.Equal(
            "Padi budidaya organik",
            result.Value.Description);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WhenCategoryMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var commodity = CreateCommodity(
            organization.Id,
            category.Id);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(
                commodity),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            commodity.Id,
            new UpdateCommodityRequest(
                "Padi Organik",
                Guid.NewGuid(),
                null,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityErrors.CategoryNotFoundCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Equal(
            category.Id,
            commodity.CommodityCategoryId);
    }

    [Fact]
    public async Task Update_WithUnchangedData_ShouldNotSave()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var commodity = Commodity.Create(
            organization.Id,
            CommodityCode.Create("PADI"),
            "Padi",
            category.Id,
            "Oryza sativa",
            "Tanaman pangan");

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(
                commodity),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            commodity.Id,
            new UpdateCommodityRequest(
                "  Padi  ",
                category.Id,
                "  Oryza sativa  ",
                "  Tanaman pangan  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_WhenInactive_ShouldActivateAndSave()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var commodity = CreateCommodity(
            organization.Id,
            category.Id);

        commodity.Deactivate();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(
                commodity),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id,
            commodity.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WhenActive_ShouldDeactivateAndSave()
    {
        var organization = CreateOrganization();

        var category = CreateCategory(
            organization.Id);

        var commodity = CreateCommodity(
            organization.Id,
            category.Id);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityRepository(
                commodity),
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.DeactivateAsync(
            organization.Id,
            commodity.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static CommodityService CreateService(
        ICommodityRepository commodityRepository,
        ICommodityCategoryRepository categoryRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        return new CommodityService(
            commodityRepository,
            categoryRepository,
            organizationRepository,
            unitOfWork);
    }

    private static Organization CreateOrganization(
        string code = "ORG-001",
        string name = "Organisasi Pertanian")
    {
        return Organization.Create(
            code,
            name);
    }

    private static CommodityCategory CreateCategory(
        Guid organizationId,
        string name = "Tanaman Pangan")
    {
        return CommodityCategory.Create(
            organizationId,
            name,
            null);
    }

    private static Commodity CreateCommodity(
        Guid organizationId,
        Guid categoryId,
        string code = "PADI",
        string name = "Padi")
    {
        return Commodity.Create(
            organizationId,
            CommodityCode.Create(code),
            name,
            categoryId,
            null,
            null);
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

        public IReadOnlyList<Commodity> Commodities =>
            _commodities;

        public Task<IReadOnlyList<Commodity>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Commodity> commodities =
                _commodities
                    .Where(commodity =>
                        commodity.OrganizationId ==
                            organizationId &&
                        !commodity.IsDeleted)
                    .OrderBy(commodity =>
                        commodity.Name)
                    .ToArray();

            return Task.FromResult(commodities);
        }

        public Task<Commodity?> GetByIdAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindCommodity(
                    organizationId,
                    commodityId));
        }

        public Task<Commodity?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindCommodity(
                    organizationId,
                    commodityId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            CommodityCode code,
            CancellationToken cancellationToken = default)
        {
            var exists = _commodities.Any(commodity =>
                commodity.OrganizationId == organizationId &&
                commodity.Code.Value == code.Value &&
                !commodity.IsDeleted);

            return Task.FromResult(exists);
        }

        public void Add(
            Commodity commodity)
        {
            _commodities.Add(commodity);
        }

        private Commodity? FindCommodity(
            Guid organizationId,
            Guid commodityId)
        {
            return _commodities.SingleOrDefault(commodity =>
                commodity.OrganizationId == organizationId &&
                commodity.Id == commodityId &&
                !commodity.IsDeleted);
        }
    }

    private sealed class FakeCommodityCategoryRepository :
        ICommodityCategoryRepository
    {
        private readonly List<CommodityCategory>
            _categories;

        public FakeCommodityCategoryRepository(
            params CommodityCategory[] categories)
        {
            _categories = categories.ToList();
        }

        public Task<IReadOnlyList<CommodityCategory>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CommodityCategory> categories =
                _categories
                    .Where(category =>
                        category.OrganizationId ==
                            organizationId &&
                        !category.IsDeleted)
                    .OrderBy(category =>
                        category.Name)
                    .ToArray();

            return Task.FromResult(categories);
        }

        public Task<CommodityCategory?> GetByIdAsync(
            Guid organizationId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindCategory(
                    organizationId,
                    categoryId));
        }

        public Task<CommodityCategory?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindCategory(
                    organizationId,
                    categoryId));
        }

        public Task<bool> NameExistsAsync(
            Guid organizationId,
            string name,
            Guid? excludedCategoryId = null,
            CancellationToken cancellationToken = default)
        {
            var exists = _categories.Any(category =>
                category.OrganizationId == organizationId &&
                category.Name == name &&
                !category.IsDeleted &&
                (
                    excludedCategoryId == null ||
                    category.Id != excludedCategoryId.Value
                ));

            return Task.FromResult(exists);
        }

        public void Add(
            CommodityCategory category)
        {
            _categories.Add(category);
        }

        private CommodityCategory? FindCategory(
            Guid organizationId,
            Guid categoryId)
        {
            return _categories.SingleOrDefault(category =>
                category.OrganizationId == organizationId &&
                category.Id == categoryId &&
                !category.IsDeleted);
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
            IReadOnlyList<Organization> organizations =
                _organizations
                    .Where(organization =>
                        !organization.IsDeleted)
                    .ToArray();

            return Task.FromResult(organizations);
        }

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var organization =
                _organizations.SingleOrDefault(candidate =>
                    candidate.Id == organizationId &&
                    !candidate.IsDeleted);

            return Task.FromResult(organization);
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
            var exists = _organizations.Any(
                organization =>
                    organization.Code == code &&
                    !organization.IsDeleted);

            return Task.FromResult(exists);
        }

        public void Add(
            Organization organization)
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
