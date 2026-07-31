using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Lands;
using SiPacul.Application.Lands.Contracts;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Application.Lands.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Application.Tests.Lands;

public sealed class LandServiceTests
{
    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateLand()
    {
        var organization = CreateOrganization();
        var repository = new FakeLandRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            repository,
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            new CreateLandRequest(
                "  lhn-001  ",
                "  Lahan Utama  ",
                LandTenureType.Leased,
                1.5m,
                AreaUnit.Hectare,
                "  Desa Sukamaju  ",
                "  Dekat saluran irigasi  ",
                -7.1m,
                110.1m,
                "  Sewa lima tahun  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("LHN-001", result.Value.Code);
        Assert.Equal(
            "Lahan Utama",
            result.Value.Name);
        Assert.Equal(
            15_000m,
            result.Value.TotalAreaInSquareMeters);
        Assert.Single(repository.Lands);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var service = CreateService(
            new FakeLandRepository(),
            new FakeOrganizationRepository(),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            CreateLandRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.OrganizationNotFoundCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.NotFound,
            result.Error.Type);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ShouldReturnConflict()
    {
        var organization = CreateOrganization();

        var existingLand = CreateLand(
            organization.Id);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeLandRepository(
                existingLand),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            CreateLandRequest(
                code: "  lhn-001  ",
                name: "Lahan Baru"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.CodeAlreadyExistsCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithInvalidArea_ShouldReturnValidation()
    {
        var organization = CreateOrganization();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeLandRepository(),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.CreateAsync(
            organization.Id,
            CreateLandRequest(
                totalArea: 0));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WithIncompleteCoordinates_ShouldReturnValidation()
    {
        var organization = CreateOrganization();

        var service = CreateService(
            new FakeLandRepository(),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var request = new CreateLandRequest(
            "LHN-001",
            "Lahan Utama",
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            null,
            null,
            -7m,
            null,
            null);

        var result = await service.CreateAsync(
            organization.Id,
            request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.Error.Type);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyOrganizationLandsOrderedByName()
    {
        var organization = CreateOrganization();

        var otherOrganization = CreateOrganization(
            "ORG-002",
            "Organisasi Lain");

        var repository = new FakeLandRepository(
            CreateLand(
                organization.Id,
                "LHN-B",
                "Beta"),
            CreateLand(
                organization.Id,
                "LHN-A",
                "Alpha"),
            CreateLand(
                otherOrganization.Id,
                "LHN-X",
                "Lahan Lain"));

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
            "Alpha",
            result.Value[0].Name);
        Assert.Equal(
            "Beta",
            result.Value[1].Name);
    }

    [Fact]
    public async Task GetAll_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var result = await CreateService(
                new FakeLandRepository(),
                new FakeOrganizationRepository(),
                new FakeUnitOfWork())
            .GetAllAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnLandWithPlots()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        land.AddPlot(
            "PTK-02",
            "Petak Barat",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.AddPlot(
            "PTK-01",
            "Petak Timur",
            3_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var service = CreateService(
            new FakeLandRepository(land),
            new FakeOrganizationRepository(
                organization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            land.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Plots.Count);
        Assert.Equal(
            "PTK-01",
            result.Value.Plots[0].Code);
        Assert.Equal(
            5_000m,
            result.Value.AllocatedPlotAreaInSquareMeters);
    }

    [Fact]
    public async Task GetById_FromOtherOrganization_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();

        var otherOrganization = CreateOrganization(
            "ORG-002",
            "Organisasi Lain");

        var otherLand = CreateLand(
            otherOrganization.Id);

        var service = CreateService(
            new FakeLandRepository(otherLand),
            new FakeOrganizationRepository(
                organization,
                otherOrganization),
            new FakeUnitOfWork());

        var result = await service.GetByIdAsync(
            organization.Id,
            otherLand.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Update_WithValidRequest_ShouldUpdateAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeLandRepository(land),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            land.Id,
            new UpdateLandRequest(
                "  Lahan Produksi  ",
                LandTenureType.Partnership,
                20_000,
                AreaUnit.SquareMeter,
                "  Desa Makmur  ",
                "  Dekat sungai  ",
                -7.5m,
                110.5m,
                "  Dikelola mitra  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Lahan Produksi",
            result.Value.Name);
        Assert.Equal(
            LandTenureType.Partnership,
            result.Value.TenureType);
        Assert.Equal(
            20_000m,
            result.Value.TotalArea);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WithUnchangedData_ShouldNotSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeLandRepository(land),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            land.Id,
            new UpdateLandRequest(
                "  Lahan Utama  ",
                LandTenureType.Owned,
                1,
                AreaUnit.Hectare,
                null,
                null,
                null,
                null,
                null));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WhenAreaBelowAllocatedPlots_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            8_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeLandRepository(land),
            new FakeOrganizationRepository(
                organization),
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            land.Id,
            new UpdateLandRequest(
                "Lahan Utama",
                LandTenureType.Owned,
                0.5m,
                AreaUnit.Hectare,
                null,
                null,
                null,
                null,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.AreaCapacityExceededCode,
            result.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            result.Error.Type);
        Assert.Equal(1m, land.TotalArea);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_WhenInactive_ShouldActivateAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        land.Deactivate();

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .ActivateAsync(
                organization.Id,
                land.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_WhenAlreadyActive_ShouldNotSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .ActivateAsync(
                organization.Id,
                land.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WhenActive_ShouldDeactivateAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .DeactivateAsync(
                organization.Id,
                land.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddPlot_WithValidRequest_ShouldAddAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .AddPlotAsync(
                organization.Id,
                land.Id,
                new AddLandPlotRequest(
                    "  ptk-01  ",
                    "  Petak Timur  ",
                    4_000,
                    AreaUnit.SquareMeter,
                    "  Tanah gembur  ",
                    "  Dekat irigasi  "));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Plots);
        Assert.Equal(
            "PTK-01",
            result.Value.Plots[0].Code);
        Assert.Equal(
            "Petak Timur",
            result.Value.Plots[0].Name);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddPlot_WithDuplicateCode_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .AddPlotAsync(
                organization.Id,
                land.Id,
                new AddLandPlotRequest(
                    "  ptk-01  ",
                    "Petak Duplikat",
                    1_000,
                    AreaUnit.SquareMeter,
                    null,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.PlotCodeAlreadyExistsCode,
            result.Error.Code);
        Assert.Single(land.Plots);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddPlot_WhenAreaExceedsLand_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            8_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .AddPlotAsync(
                organization.Id,
                land.Id,
                new AddLandPlotRequest(
                    "PTK-02",
                    "Petak Dua",
                    3_000,
                    AreaUnit.SquareMeter,
                    null,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.AreaCapacityExceededCode,
            result.Error.Code);
        Assert.Single(land.Plots);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdatePlot_WithValidRequest_ShouldUpdateAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .UpdatePlotAsync(
                organization.Id,
                land.Id,
                plot.Id,
                new UpdateLandPlotRequest(
                    "  Petak Timur  ",
                    0.3m,
                    AreaUnit.Hectare,
                    "  Tanah liat  ",
                    "  Perlu drainase  "));

        Assert.True(result.IsSuccess);

        var response = result.Value.Plots.Single();

        Assert.Equal(
            "Petak Timur",
            response.Name);
        Assert.Equal(0.3m, response.Area);
        Assert.Equal(
            AreaUnit.Hectare,
            response.AreaUnit);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdatePlot_WithUnchangedData_ShouldNotSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            "Tanah gembur",
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .UpdatePlotAsync(
                organization.Id,
                land.Id,
                plot.Id,
                new UpdateLandPlotRequest(
                    "  Petak Satu  ",
                    2_000,
                    AreaUnit.SquareMeter,
                    "  Tanah gembur  ",
                    "   "));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdatePlot_WhenPlotMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                new FakeUnitOfWork())
            .UpdatePlotAsync(
                organization.Id,
                land.Id,
                Guid.NewGuid(),
                new UpdateLandPlotRequest(
                    "Petak Tidak Ada",
                    1_000,
                    AreaUnit.SquareMeter,
                    null,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.PlotNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdatePlot_WhenAreaExceedsLand_ShouldReturnConflictWithoutMutation()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var first = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            6_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.AddPlot(
            "PTK-02",
            "Petak Dua",
            3_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .UpdatePlotAsync(
                organization.Id,
                land.Id,
                first.Id,
                new UpdateLandPlotRequest(
                    "Petak Besar",
                    8_000,
                    AreaUnit.SquareMeter,
                    null,
                    null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.AreaCapacityExceededCode,
            result.Error.Code);
        Assert.Equal("Petak Satu", first.Name);
        Assert.Equal(6_000m, first.Area);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemovePlot_WhenFound_ShouldRemoveAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .RemovePlotAsync(
                organization.Id,
                land.Id,
                plot.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Plots);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemovePlot_WhenMissing_ShouldReturnNotFound()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .RemovePlotAsync(
                organization.Id,
                land.Id,
                Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            LandErrors.PlotNotFoundCode,
            result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ActivatePlot_WhenInactive_ShouldActivateAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.DeactivatePlot(plot.Id);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .ActivatePlotAsync(
                organization.Id,
                land.Id,
                plot.Id);

        Assert.True(result.IsSuccess);
        Assert.True(
            result.Value.Plots.Single().IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeactivatePlot_WhenActive_ShouldDeactivateAndSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .DeactivatePlotAsync(
                organization.Id,
                land.Id,
                plot.Id);

        Assert.True(result.IsSuccess);
        Assert.False(
            result.Value.Plots.Single().IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeactivatePlot_WhenAlreadyInactive_ShouldNotSave()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.DeactivatePlot(plot.Id);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork)
            .DeactivatePlotAsync(
                organization.Id,
                land.Id,
                plot.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_WithActiveCropCycle_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            5_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var cropCycle = CreateCropCycle(
            organization.Id,
            land.Id,
            plot.Id);

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork,
                new FakeCropCycleRepository(
                    cropCycle))
            .DeactivateAsync(
                organization.Id,
                land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.ActiveReferenceExistsCode,
            result.Error.Code);
        Assert.True(land.IsActive);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeactivatePlot_WithActiveCropCycle_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            5_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var cropCycle = CreateCropCycle(
            organization.Id,
            land.Id,
            plot.Id);

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                new FakeUnitOfWork(),
                new FakeCropCycleRepository(
                    cropCycle))
            .DeactivatePlotAsync(
                organization.Id,
                land.Id,
                plot.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.ActiveReferenceExistsCode,
            result.Error.Code);
        Assert.True(plot.IsActive);
    }

    [Fact]
    public async Task RemovePlot_WithHistoricalCropCycle_ShouldReturnConflict()
    {
        var organization = CreateOrganization();
        var land = CreateLand(organization.Id);

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            5_000,
            AreaUnit.SquareMeter,
            null,
            null);

        var cropCycle = CreateCropCycle(
            organization.Id,
            land.Id,
            plot.Id);

        cropCycle.Cancel("Rencana dibatalkan");

        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateService(
                new FakeLandRepository(land),
                new FakeOrganizationRepository(
                    organization),
                unitOfWork,
                new FakeCropCycleRepository(
                    cropCycle))
            .RemovePlotAsync(
                organization.Id,
                land.Id,
                plot.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CropCycleErrors.HistoricalReferenceExistsCode,
            result.Error.Code);
        Assert.Single(land.Plots);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static LandService CreateService(
        ILandRepository landRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        ICropCycleRepository? cropCycleRepository = null)
    {
        return new LandService(
            landRepository,
            cropCycleRepository ??
                new FakeCropCycleRepository(),
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

    private static Land CreateLand(
        Guid organizationId,
        string code = "LHN-001",
        string name = "Lahan Utama")
    {
        return Land.Create(
            organizationId,
            code,
            name,
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            null,
            null,
            null,
            null,
            null);
    }

    private static CreateLandRequest CreateLandRequest(
        string code = "LHN-001",
        string name = "Lahan Utama",
        decimal totalArea = 1)
    {
        return new CreateLandRequest(
            code,
            name,
            LandTenureType.Owned,
            totalArea,
            AreaUnit.Hectare,
            null,
            null,
            null,
            null,
            null);
    }

    private static CropCycle CreateCropCycle(
        Guid organizationId,
        Guid landId,
        Guid landPlotId)
    {
        return CropCycle.Create(
            organizationId,
            "SC-PADI-001",
            "Musim Tanam Padi",
            Guid.NewGuid(),
            null,
            landId,
            landPlotId,
            4_000,
            AreaUnit.SquareMeter,
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 5, 1),
            null);
    }

    private sealed class FakeLandRepository :
        ILandRepository
    {
        private readonly List<Land> _lands;

        public FakeLandRepository(
            params Land[] lands)
        {
            _lands = lands.ToList();
        }

        public IReadOnlyList<Land> Lands =>
            _lands;

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
                    .OrderBy(land => land.Name)
                    .ThenBy(land => land.Code)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<Land?> GetByIdAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindLand(
                    organizationId,
                    landId));
        }

        public Task<Land?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                FindLand(
                    organizationId,
                    landId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            var exists = _lands.Any(land =>
                land.OrganizationId ==
                    organizationId &&
                land.Code == code &&
                !land.IsDeleted);

            return Task.FromResult(exists);
        }

        public void Add(Land land)
        {
            _lands.Add(land);
        }

        private Land? FindLand(
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

        public void Add(
            Organization organization)
        {
            _organizations.Add(organization);
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
            return Task.FromResult(false);
        }

        public Task<bool> HasInProgressCycleAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            Guid? excludedCropCycleId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _cropCycles.Any(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    cropCycle.Status ==
                        CropCycleStatus.InProgress &&
                    !cropCycle.IsDeleted));
        }

        public Task<bool> HasActiveCycleForLandAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _cropCycles.Any(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    IsActive(cropCycle)));
        }

        public Task<bool> HasActiveCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _cropCycles.Any(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    IsActive(cropCycle)));
        }

        public Task<bool> HasAnyCycleForPlotAsync(
            Guid organizationId,
            Guid landId,
            Guid landPlotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _cropCycles.Any(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    !cropCycle.IsDeleted));
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

        private static bool IsActive(
            CropCycle cropCycle)
        {
            return !cropCycle.IsDeleted &&
                (
                    cropCycle.Status ==
                        CropCycleStatus.Planned ||
                    cropCycle.Status ==
                        CropCycleStatus.InProgress
                );
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
