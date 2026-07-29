using SiPacul.Application.Common.Persistence;
using SiPacul.Application.MasterData.CommodityCategories;
using SiPacul.Application.MasterData.CommodityCategories.Contracts;
using SiPacul.Application.MasterData.CommodityCategories.Persistence;
using SiPacul.Application.MasterData.CommodityCategories.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.MasterData.CommodityCategories;

public sealed class CommodityCategoryServiceTests
{
    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateCategory()
    {
        var organization = CreateOrganization();

        var categoryRepository =
            new FakeCommodityCategoryRepository();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            categoryRepository,
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var request =
            new CreateCommodityCategoryRequest(
                "  Tanaman Buah  ",
                "  Tanaman penghasil buah  ");

        var result = await service.CreateAsync(
            organization.Id,
            request);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            organization.Id,
            result.Value.OrganizationId);
        Assert.Equal(
            "Tanaman Buah",
            result.Value.Name);
        Assert.Equal(
            "Tanaman penghasil buah",
            result.Value.Description);
        Assert.Single(categoryRepository.Categories);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var service = CreateService(
            new FakeCommodityCategoryRepository(),
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            new CreateCommodityCategoryRequest(
                "Tanaman Buah",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityCategoryErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ShouldReturnConflict()
    {
        var organization = CreateOrganization();

        var existingCategory =
            CommodityCategory.Create(
                organization.Id,
                "Tanaman Buah",
                null);

        var categoryRepository =
            new FakeCommodityCategoryRepository(
                existingCategory);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            categoryRepository,
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCommodityCategoryRequest(
                "Tanaman Buah",
                "Kategori duplikat"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityCategoryErrors
                .NameAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
        Assert.Single(categoryRepository.Categories);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithInvalidName_ShouldReturnValidation()
    {
        var organization = CreateOrganization();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityCategoryRepository(),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCommodityCategoryRequest(
                "   ",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyOrganizationCategoriesOrderedByName()
    {
        var organization = CreateOrganization();
        var otherOrganization = CreateOrganization(
            "ORG-002",
            "Organisasi Lain");

        var repository =
            new FakeCommodityCategoryRepository(
                CommodityCategory.Create(
                    organization.Id,
                    "Tanaman Sayur",
                    null),
                CommodityCategory.Create(
                    organization.Id,
                    "Tanaman Buah",
                    null),
                CommodityCategory.Create(
                    otherOrganization.Id,
                    "Peternakan",
                    null));

        var service = CreateService(
            repository,
            new FakeOrganizationRepository(
                organization,
                otherOrganization),
            new FakeUnitOfWork());

        var result = await service.GetAllAsync(
            organization.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(
            "Tanaman Buah",
            result.Value[0].Name);
        Assert.Equal(
            "Tanaman Sayur",
            result.Value[1].Name);
    }

    [Fact]
    public async Task GetById_WhenCategoryExists_ShouldReturnCategory()
    {
        var organization = CreateOrganization();

        var category = CommodityCategory.Create(
            organization.Id,
            "Tanaman Buah",
            null);

        var service = CreateService(
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            category.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            category.Id,
            result.Value.Id);
    }

    [Fact]
    public async Task GetById_WhenCategoryMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var service = CreateService(
            new FakeCommodityCategoryRepository(),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CommodityCategoryErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Update_WithValidRequest_ShouldUpdateAndSave()
    {
        var organization = CreateOrganization();

        var category = CommodityCategory.Create(
            organization.Id,
            "Tanaman",
            "Deskripsi awal");

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            category.Id,
            new UpdateCommodityCategoryRequest(
                "  Tanaman Perkebunan  ",
                "  Deskripsi baru  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Tanaman Perkebunan",
            result.Value.Name);
        Assert.Equal(
            "Deskripsi baru",
            result.Value.Description);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WithDuplicateName_ShouldReturnConflict()
    {
        var organization = CreateOrganization();

        var firstCategory =
            CommodityCategory.Create(
                organization.Id,
                "Tanaman Buah",
                null);

        var secondCategory =
            CommodityCategory.Create(
                organization.Id,
                "Tanaman Sayur",
                null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityCategoryRepository(
                firstCategory,
                secondCategory),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            secondCategory.Id,
            new UpdateCommodityCategoryRequest(
                "Tanaman Buah",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
        Assert.Equal(
            "Tanaman Sayur",
            secondCategory.Name);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WithUnchangedData_ShouldNotSave()
    {
        var organization = CreateOrganization();

        var category = CommodityCategory.Create(
            organization.Id,
            "Tanaman Buah",
            "Deskripsi");

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            category.Id,
            new UpdateCommodityCategoryRequest(
                "  Tanaman Buah  ",
                "  Deskripsi  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_WhenInactive_ShouldActivateAndSave()
    {
        var organization = CreateOrganization();

        var category = CommodityCategory.Create(
            organization.Id,
            "Tanaman Buah",
            null);

        category.Deactivate();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id,
            category.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WhenActive_ShouldDeactivateAndSave()
    {
        var organization = CreateOrganization();

        var category = CommodityCategory.Create(
            organization.Id,
            "Tanaman Buah",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCommodityCategoryRepository(
                category),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.DeactivateAsync(
            organization.Id,
            category.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static CommodityCategoryService CreateService(
        ICommodityCategoryRepository categoryRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        return new CommodityCategoryService(
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

        public IReadOnlyList<CommodityCategory> Categories =>
            _categories;

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
