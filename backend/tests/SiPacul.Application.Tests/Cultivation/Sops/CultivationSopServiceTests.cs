using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.Sops;
using SiPacul.Application.Cultivation.Sops.Contracts;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.Cultivation.Sops.Services;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Cultivation.Sops;

public sealed class CultivationSopServiceTests
{
    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateSop()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var repository =
            new FakeCultivationSopRepository();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            repository,
            new FakeCommodityRepository(
                commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCultivationSopRequest(
                commodity.Id,
                "  SOP Budidaya Padi  ",
                "  Panduan standar budidaya padi.  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "SOP Budidaya Padi",
            result.Value.Name);
        Assert.Equal(
            commodity.Id,
            result.Value.CommodityId);
        Assert.Equal(
            "Panduan standar budidaya padi.",
            result.Value.Description);
        Assert.Single(repository.Sops);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var organizationId = Guid.NewGuid();
        var commodity = CreateCommodity(
            organizationId);

        var service = CreateService(
            new FakeCultivationSopRepository(),
            new FakeCommodityRepository(
                commodity),
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            organizationId,
            new CreateCultivationSopRequest(
                commodity.Id,
                "SOP Padi",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationSopErrors
                .OrganizationNotFoundCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);
    }

    [Fact]
    public async Task Create_WhenCommodityMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var service = CreateService(
            new FakeCultivationSopRepository(),
            new FakeCommodityRepository(),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCultivationSopRequest(
                Guid.NewGuid(),
                "SOP Padi",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationSopErrors.CommodityNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var existingSop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(
                existingSop),
            new FakeCommodityRepository(
                commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCultivationSopRequest(
                commodity.Id,
                "  SOP Padi  ",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationSopErrors.NameAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithInvalidName_ShouldReturnValidation()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(),
            new FakeCommodityRepository(
                commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateCultivationSopRequest(
                commodity.Id,
                "   ",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByOrganizationAndCommodity()
    {
        var organization = CreateOrganization();

        var otherOrganization = CreateOrganization(
            "ORG-002",
            "Organisasi Lain");

        var firstCommodity = CreateCommodity(
            organization.Id,
            "PADI",
            "Padi");

        var secondCommodity = CreateCommodity(
            organization.Id,
            "JAGUNG",
            "Jagung");

        var otherCommodity = CreateCommodity(
            otherOrganization.Id,
            "PADI",
            "Padi");

        var firstSop = CultivationSop.Create(
            organization.Id,
            firstCommodity.Id,
            "SOP Padi",
            null);

        var secondSop = CultivationSop.Create(
            organization.Id,
            secondCommodity.Id,
            "SOP Jagung",
            null);

        var otherSop = CultivationSop.Create(
            otherOrganization.Id,
            otherCommodity.Id,
            "SOP Padi Lain",
            null);

        var service = CreateService(
            new FakeCultivationSopRepository(
                firstSop,
                secondSop,
                otherSop),
            new FakeCommodityRepository(
                firstCommodity,
                secondCommodity,
                otherCommodity),
            new FakeOrganizationRepository(
                organization,
                otherOrganization),
            new FakeUnitOfWork());

        var result = await service.GetAllAsync(
            organization.Id,
            firstCommodity.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(
            firstSop.Id,
            result.Value[0].Id);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOrderedSteps()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        sop.AddStep(
            "Persiapan",
            null,
            -14,
            7,
            true);

        sop.AddStep(
            "Penanaman",
            null,
            0,
            1,
            true);

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            sop.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Steps.Count);
        Assert.Equal(
            1,
            result.Value.Steps[0].Sequence);
        Assert.Equal(
            2,
            result.Value.Steps[1].Sequence);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var service = CreateService(
            new FakeCultivationSopRepository(),
            new FakeCommodityRepository(),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationSopErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Update_WithValidRequest_ShouldUpdateAndSave()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            sop.Id,
            new UpdateCultivationSopRequest(
                "  SOP Padi Organik  ",
                "  Panduan organik  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "SOP Padi Organik",
            result.Value.Name);
        Assert.Equal(
            "Panduan organik",
            result.Value.Description);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WithDuplicateName_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var firstSop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var secondSop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi Organik",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(
                firstSop,
                secondSop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            firstSop.Id,
            new UpdateCultivationSopRequest(
                "SOP Padi Organik",
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationSopErrors.NameAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Equal("SOP Padi", firstSop.Name);
    }

    [Fact]
    public async Task Activate_WhenInactive_ShouldActivateAndSave()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        sop.Deactivate();

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.ActivateAsync(
            organization.Id,
            sop.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WhenActive_ShouldDeactivateAndSave()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.DeactivateAsync(
            organization.Id,
            sop.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddStep_WithValidRequest_ShouldAddAndSave()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.AddStepAsync(
            organization.Id,
            sop.Id,
            new AddCultivationSopStepRequest(
                "  Persiapan Lahan  ",
                "  Bersihkan lahan.  ",
                -14,
                7,
                true));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Steps);
        Assert.Equal(
            "Persiapan Lahan",
            result.Value.Steps[0].Name);
        Assert.Equal(
            1,
            result.Value.Steps[0].Sequence);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddStep_WithInvalidDuration_ShouldReturnValidation()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.AddStepAsync(
            organization.Id,
            sop.Id,
            new AddCultivationSopStepRequest(
                "Persiapan",
                null,
                0,
                0,
                true));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
        Assert.Empty(sop.Steps);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateStep_WithValidRequest_ShouldUpdateAndSave()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var step = sop.AddStep(
            "Pemupukan",
            null,
            7,
            1,
            true);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateStepAsync(
            organization.Id,
            sop.Id,
            step.Id,
            new UpdateCultivationSopStepRequest(
                "  Pemupukan Pertama  ",
                "  Gunakan pupuk dasar.  ",
                10,
                2,
                false));

        Assert.True(result.IsSuccess);

        var updatedStep =
            result.Value.Steps.Single();

        Assert.Equal(
            "Pemupukan Pertama",
            updatedStep.Name);
        Assert.Equal(10, updatedStep.PlannedDayOffset);
        Assert.Equal(
            2,
            updatedStep.EstimatedDurationDays);
        Assert.False(updatedStep.IsRequired);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateStep_WhenMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.UpdateStepAsync(
            organization.Id,
            sop.Id,
            Guid.NewGuid(),
            new UpdateCultivationSopStepRequest(
                "Pemupukan",
                null,
                7,
                1,
                true));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CultivationSopErrors.StepNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task RemoveStep_ShouldRemoveAndResequence()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        sop.AddStep(
            "Persiapan",
            null,
            -14,
            7,
            true);

        var secondStep = sop.AddStep(
            "Penanaman",
            null,
            0,
            1,
            true);

        sop.AddStep(
            "Pemupukan",
            null,
            7,
            1,
            true);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.RemoveStepAsync(
            organization.Id,
            sop.Id,
            secondStep.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Steps.Count);
        Assert.Equal(
            1,
            result.Value.Steps[0].Sequence);
        Assert.Equal(
            2,
            result.Value.Steps[1].Sequence);
        Assert.Equal(
            "Pemupukan",
            result.Value.Steps[1].Name);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task MoveStep_ShouldReorderAndResequence()
    {
        var organization = CreateOrganization();
        var commodity = CreateCommodity(
            organization.Id);

        var sop = CultivationSop.Create(
            organization.Id,
            commodity.Id,
            "SOP Padi",
            null);

        sop.AddStep(
            "Persiapan",
            null,
            -14,
            7,
            true);

        sop.AddStep(
            "Penanaman",
            null,
            0,
            1,
            true);

        var thirdStep = sop.AddStep(
            "Pemupukan",
            null,
            7,
            1,
            true);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCultivationSopRepository(sop),
            new FakeCommodityRepository(commodity),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.MoveStepAsync(
            organization.Id,
            sop.Id,
            thirdStep.Id,
            new MoveCultivationSopStepRequest(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            thirdStep.Id,
            result.Value.Steps[0].Id);
        Assert.Equal(
            1,
            result.Value.Steps[0].Sequence);
        Assert.Equal(
            "Persiapan",
            result.Value.Steps[1].Name);
        Assert.Equal(
            2,
            result.Value.Steps[1].Sequence);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static CultivationSopService CreateService(
        ICultivationSopRepository cultivationSopRepository,
        ICommodityRepository commodityRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        return new CultivationSopService(
            cultivationSopRepository,
            commodityRepository,
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

    private static Commodity CreateCommodity(
        Guid organizationId,
        string code = "PADI",
        string name = "Padi")
    {
        return Commodity.Create(
            organizationId,
            CommodityCode.Create(code),
            name,
            Guid.NewGuid(),
            null,
            null);
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

        public IReadOnlyList<CultivationSop> Sops =>
            _sops;

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
                    .OrderBy(sop => sop.Name)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<CultivationSop?> GetByIdAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindSop(
                    organizationId,
                    cultivationSopId));
        }

        public Task<CultivationSop?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindSop(
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
            var exists = _sops.Any(sop =>
                sop.OrganizationId == organizationId &&
                sop.CommodityId == commodityId &&
                sop.Name == name &&
                !sop.IsDeleted &&
                (
                    excludedCultivationSopId == null ||
                    sop.Id !=
                        excludedCultivationSopId.Value
                ));

            return Task.FromResult(exists);
        }

        public void Add(
            CultivationSop cultivationSop)
        {
            _sops.Add(cultivationSop);
        }

        private CultivationSop? FindSop(
            Guid organizationId,
            Guid cultivationSopId)
        {
            return _sops.SingleOrDefault(sop =>
                sop.OrganizationId == organizationId &&
                sop.Id == cultivationSopId &&
                !sop.IsDeleted);
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
                    .OrderBy(commodity =>
                        commodity.Name)
                    .ToArray();

            return Task.FromResult(result);
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
                commodity.OrganizationId ==
                    organizationId &&
                commodity.Code == code &&
                !commodity.IsDeleted);

            return Task.FromResult(exists);
        }

        public void Add(Commodity commodity)
        {
            _commodities.Add(commodity);
        }

        private Commodity? FindCommodity(
            Guid organizationId,
            Guid commodityId)
        {
            return _commodities.SingleOrDefault(commodity =>
                commodity.OrganizationId ==
                    organizationId &&
                commodity.Id == commodityId &&
                !commodity.IsDeleted);
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
