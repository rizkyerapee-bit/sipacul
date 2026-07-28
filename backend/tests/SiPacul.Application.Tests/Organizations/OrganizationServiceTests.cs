using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Organizations;
using SiPacul.Application.Organizations.Contracts;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Application.Organizations.Services;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Organizations;

public sealed class OrganizationServiceTests
{
    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateOrganization()
    {
        var repository =
            new FakeOrganizationRepository();

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            repository,
            unitOfWork);

        var request = new CreateOrganizationRequest(
            "  org-001  ",
            "  Bisnis Pertanian  ",
            "  PT Bisnis Pertanian  ",
            null);

        var result = await service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("ORG-001", result.Value.Code);
        Assert.Equal(
            "Bisnis Pertanian",
            result.Value.Name);

        Assert.Single(repository.Organizations);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ShouldReturnConflict()
    {
        var existingOrganization =
            Organization.Create(
                "ORG-001",
                "Organisasi Pertama");

        var repository =
            new FakeOrganizationRepository(
                existingOrganization);

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            repository,
            unitOfWork);

        var request = new CreateOrganizationRequest(
            "org-001",
            "Organisasi Kedua",
            null,
            null);

        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationErrors.CodeAlreadyExistsCode,
            result.Error.Code);

        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);

        Assert.Single(repository.Organizations);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithInvalidCode_ShouldReturnValidation()
    {
        var repository =
            new FakeOrganizationRepository();

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            repository,
            unitOfWork);

        var request = new CreateOrganizationRequest(
            "ORG 001",
            "Organisasi",
            null,
            null);

        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);

        Assert.Empty(repository.Organizations);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOrganizationsOrderedByName()
    {
        var repository =
            new FakeOrganizationRepository(
                Organization.Create(
                    "ORG-B",
                    "Beta"),
                Organization.Create(
                    "ORG-A",
                    "Alpha"));

        var service = new OrganizationService(
            repository,
            new FakeUnitOfWork());

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Equal(
            "Alpha",
            result.Value[0].Name);

        Assert.Equal(
            "Beta",
            result.Value[1].Name);
    }

    [Fact]
    public async Task GetById_WhenOrganizationExists_ShouldReturnOrganization()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi");

        var repository =
            new FakeOrganizationRepository(
                organization);

        var service = new OrganizationService(
            repository,
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            organization.Id,
            result.Value.Id);
    }

    [Fact]
    public async Task GetById_WhenOrganizationDoesNotExist_ShouldReturnNotFound()
    {
        var service = new OrganizationService(
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);
    }

    [Fact]
    public async Task Update_WhenOrganizationExists_ShouldUpdateAndSave()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Nama Awal");

        var repository =
            new FakeOrganizationRepository(
                organization);

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            repository,
            unitOfWork);

        var request = new UpdateOrganizationRequest(
            "  Nama Baru  ",
            "  PT Nama Baru  ",
            "  Asia/Makassar  ");

        var result = await service.UpdateAsync(
            organization.Id,
            request);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Nama Baru",
            result.Value.Name);

        Assert.Equal(
            "PT Nama Baru",
            result.Value.LegalName);

        Assert.Equal(
            "Asia/Makassar",
            result.Value.TimeZone);

        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WithUnchangedData_ShouldNotSave()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi",
                null,
                "Asia/Jakarta");

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var request = new UpdateOrganizationRequest(
            "  Organisasi  ",
            "   ",
            "  Asia/Jakarta  ");

        var result = await service.UpdateAsync(
            organization.Id,
            request);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_WhenInactive_ShouldActivateAndSave()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi");

        organization.Deactivate();

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_WhenAlreadyActive_ShouldNotSave()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi");

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WhenActive_ShouldDeactivateAndSave()
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi");

        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.DeactivateAsync(
            organization.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WhenOrganizationDoesNotExist_ShouldReturnNotFound()
    {
        var unitOfWork = new FakeUnitOfWork();

        var service = new OrganizationService(
            new FakeOrganizationRepository(),
            unitOfWork);

        var result = await service.DeactivateAsync(
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);

        Assert.Equal(0, unitOfWork.SaveCount);
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

        public IReadOnlyList<Organization> Organizations =>
            _organizations;

        public Task<IReadOnlyList<Organization>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Organization> organizations =
                _organizations
                    .Where(organization =>
                        !organization.IsDeleted)
                    .OrderBy(organization =>
                        organization.Name)
                    .ThenBy(organization =>
                        organization.Code)
                    .ToArray();

            return Task.FromResult(organizations);
        }

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var organization = _organizations
                .SingleOrDefault(candidate =>
                    candidate.Id == organizationId &&
                    !candidate.IsDeleted);

            return Task.FromResult(organization);
        }

        public Task<Organization?> GetByIdForUpdateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var organization = _organizations
                .SingleOrDefault(candidate =>
                    candidate.Id == organizationId &&
                    !candidate.IsDeleted);

            return Task.FromResult(organization);
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
