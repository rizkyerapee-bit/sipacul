using SiPacul.Domain.Entities.Lands;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Lands;

public sealed class LandTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateLand()
    {
        var organizationId = Guid.NewGuid();

        var land = Land.Create(
            organizationId,
            "  lhn-001  ",
            "  Lahan Utama  ",
            LandTenureType.Leased,
            1.5m,
            AreaUnit.Hectare,
            "  Desa Sukamaju  ",
            "  Sebelah utara jalan desa  ",
            -7.123456m,
            110.123456m,
            "  Sewa lima tahun  ");

        Assert.NotEqual(Guid.Empty, land.Id);
        Assert.Equal(
            organizationId,
            land.OrganizationId);
        Assert.Equal("LHN-001", land.Code);
        Assert.Equal("Lahan Utama", land.Name);
        Assert.Equal(
            LandTenureType.Leased,
            land.TenureType);
        Assert.Equal(1.5m, land.TotalArea);
        Assert.Equal(
            AreaUnit.Hectare,
            land.AreaUnit);
        Assert.Equal(
            15_000m,
            land.TotalAreaInSquareMeters);
        Assert.Equal(
            "Desa Sukamaju",
            land.Address);
        Assert.Equal(
            "Sebelah utara jalan desa",
            land.LocationDescription);
        Assert.Equal(-7.123456m, land.Latitude);
        Assert.Equal(110.123456m, land.Longitude);
        Assert.Equal(
            "Sewa lima tahun",
            land.Notes);
        Assert.True(land.IsActive);
        Assert.False(land.IsDeleted);
        Assert.Empty(land.Plots);
        Assert.Null(land.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateLand(
                    organizationId: Guid.Empty));

        Assert.Equal(
            "organizationId",
            exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_ShouldThrow(
        string invalidCode)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateLand(code: invalidCode));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("LHN 001")]
    [InlineData("LHN/001")]
    [InlineData("LHN.001")]
    public void Create_WithInvalidCode_ShouldThrow(
        string invalidCode)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateLand(code: invalidCode));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_WithCodeExceedingMaximum_ShouldThrow()
    {
        var invalidCode = new string(
            'A',
            Land.MaxCodeLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateLand(code: invalidCode));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyName_ShouldThrow(
        string invalidName)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateLand(name: invalidName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithNameExceedingMaximum_ShouldThrow()
    {
        var invalidName = new string(
            'A',
            Land.MaxNameLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateLand(name: invalidName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithNonPositiveArea_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateLand(totalArea: 0));

        Assert.Equal("area", exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedAreaUnit_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateLand(
                    areaUnit: (AreaUnit)999));

        Assert.Equal("unit", exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedTenureType_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateLand(
                    tenureType:
                        (LandTenureType)999));

        Assert.Equal(
            "tenureType",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithWhitespaceOptionalText_ShouldSetNull()
    {
        var land = Land.Create(
            Guid.NewGuid(),
            "LHN-001",
            "Lahan Utama",
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            "   ",
            "   ",
            null,
            null,
            "   ");

        Assert.Null(land.Address);
        Assert.Null(land.LocationDescription);
        Assert.Null(land.Latitude);
        Assert.Null(land.Longitude);
        Assert.Null(land.Notes);
    }

    [Fact]
    public void Create_WithOnlyLatitude_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                Land.Create(
                    Guid.NewGuid(),
                    "LHN-001",
                    "Lahan Utama",
                    LandTenureType.Owned,
                    1,
                    AreaUnit.Hectare,
                    null,
                    null,
                    -7m,
                    null,
                    null));

        Assert.Equal(
            "latitude",
            exception.ParamName);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Create_WithInvalidLatitude_ShouldThrow(
        decimal invalidLatitude)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Land.Create(
                    Guid.NewGuid(),
                    "LHN-001",
                    "Lahan Utama",
                    LandTenureType.Owned,
                    1,
                    AreaUnit.Hectare,
                    null,
                    null,
                    invalidLatitude,
                    110,
                    null));

        Assert.Equal(
            "latitude",
            exception.ParamName);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Create_WithInvalidLongitude_ShouldThrow(
        decimal invalidLongitude)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Land.Create(
                    Guid.NewGuid(),
                    "LHN-001",
                    "Lahan Utama",
                    LandTenureType.Owned,
                    1,
                    AreaUnit.Hectare,
                    null,
                    null,
                    -7,
                    invalidLongitude,
                    null));

        Assert.Equal(
            "longitude",
            exception.ParamName);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateLand()
    {
        var land = CreateLand();

        land.Update(
            "  Lahan Produksi Utama  ",
            LandTenureType.Partnership,
            20_000,
            AreaUnit.SquareMeter,
            "  Desa Makmur  ",
            "  Dekat saluran irigasi  ",
            -7.5m,
            110.5m,
            "  Dikelola bersama mitra  ");

        Assert.Equal(
            "Lahan Produksi Utama",
            land.Name);
        Assert.Equal(
            LandTenureType.Partnership,
            land.TenureType);
        Assert.Equal(20_000m, land.TotalArea);
        Assert.Equal(
            AreaUnit.SquareMeter,
            land.AreaUnit);
        Assert.Equal(
            "Desa Makmur",
            land.Address);
        Assert.Equal(
            "Dekat saluran irigasi",
            land.LocationDescription);
        Assert.Equal(-7.5m, land.Latitude);
        Assert.Equal(110.5m, land.Longitude);
        Assert.Equal(
            "Dikelola bersama mitra",
            land.Notes);
        Assert.NotNull(land.UpdatedAt);
    }

    [Fact]
    public void Update_WithUnchangedData_ShouldNotSetUpdatedAt()
    {
        var land = CreateLand();

        land.Update(
            "  Lahan Utama  ",
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            null,
            null,
            null,
            null,
            null);

        Assert.Null(land.UpdatedAt);
    }

    [Fact]
    public void Update_ReducingAreaBelowAllocatedPlots_ShouldThrow()
    {
        var land = CreateLand();

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            8_000,
            AreaUnit.SquareMeter,
            null,
            null);

        Assert.Throws<InvalidOperationException>(() =>
            land.Update(
                "Lahan Utama",
                LandTenureType.Owned,
                0.5m,
                AreaUnit.Hectare,
                null,
                null,
                null,
                null,
                null));

        Assert.Equal(1m, land.TotalArea);
        Assert.Equal(
            AreaUnit.Hectare,
            land.AreaUnit);
    }

    [Fact]
    public void AddPlot_WithValidData_ShouldAddPlot()
    {
        var land = CreateLand();

        var plot = land.AddPlot(
            "  ptk-01  ",
            "  Petak Timur  ",
            4_000,
            AreaUnit.SquareMeter,
            "  Tanah gembur  ",
            "  Akses dekat irigasi  ");

        Assert.Single(land.Plots);
        Assert.NotEqual(Guid.Empty, plot.Id);
        Assert.Equal(
            land.OrganizationId,
            plot.OrganizationId);
        Assert.Equal(land.Id, plot.LandId);
        Assert.Equal("PTK-01", plot.Code);
        Assert.Equal("Petak Timur", plot.Name);
        Assert.Equal(4_000m, plot.Area);
        Assert.Equal(
            AreaUnit.SquareMeter,
            plot.AreaUnit);
        Assert.Equal(
            "Tanah gembur",
            plot.GeneralCondition);
        Assert.Equal(
            "Akses dekat irigasi",
            plot.Notes);
        Assert.True(plot.IsActive);
        Assert.Equal(
            4_000m,
            land.AllocatedPlotAreaInSquareMeters);
        Assert.NotNull(land.UpdatedAt);
    }

    [Fact]
    public void AddPlot_WithDuplicateCode_ShouldThrow()
    {
        var land = CreateLand();

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        Assert.Throws<InvalidOperationException>(() =>
            land.AddPlot(
                "  ptk-01  ",
                "Petak Duplikat",
                1_000,
                AreaUnit.SquareMeter,
                null,
                null));

        Assert.Single(land.Plots);
    }

    [Fact]
    public void AddPlot_ExceedingTotalArea_ShouldThrow()
    {
        var land = CreateLand();

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            7_000,
            AreaUnit.SquareMeter,
            null,
            null);

        Assert.Throws<InvalidOperationException>(() =>
            land.AddPlot(
                "PTK-02",
                "Petak Dua",
                4_000,
                AreaUnit.SquareMeter,
                null,
                null));

        Assert.Single(land.Plots);
    }

    [Fact]
    public void AddPlot_WithMixedUnits_ShouldConvertArea()
    {
        var land = CreateLand();

        land.AddPlot(
            "PTK-01",
            "Petak Satu",
            5_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.AddPlot(
            "PTK-02",
            "Petak Dua",
            0.5m,
            AreaUnit.Hectare,
            null,
            null);

        Assert.Equal(2, land.Plots.Count);
        Assert.Equal(
            10_000m,
            land.AllocatedPlotAreaInSquareMeters);
    }

    [Fact]
    public void UpdatePlot_WithValidData_ShouldUpdatePlot()
    {
        var land = CreateLand();

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            2_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.UpdatePlot(
            plot.Id,
            "  Petak Timur  ",
            0.3m,
            AreaUnit.Hectare,
            "  Tanah liat  ",
            "  Perlu drainase  ");

        Assert.Equal("Petak Timur", plot.Name);
        Assert.Equal(0.3m, plot.Area);
        Assert.Equal(
            AreaUnit.Hectare,
            plot.AreaUnit);
        Assert.Equal(
            "Tanah liat",
            plot.GeneralCondition);
        Assert.Equal(
            "Perlu drainase",
            plot.Notes);
        Assert.Equal(
            3_000m,
            land.AllocatedPlotAreaInSquareMeters);
        Assert.NotNull(plot.UpdatedAt);
        Assert.NotNull(land.UpdatedAt);
    }

    [Fact]
    public void UpdatePlot_ExceedingAvailableArea_ShouldThrowWithoutMutation()
    {
        var land = CreateLand();

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

        Assert.Throws<InvalidOperationException>(() =>
            land.UpdatePlot(
                first.Id,
                "Petak Satu Besar",
                8_000,
                AreaUnit.SquareMeter,
                null,
                null));

        Assert.Equal("Petak Satu", first.Name);
        Assert.Equal(6_000m, first.Area);
    }

    [Fact]
    public void RemovePlot_ShouldRemovePlotAndReleaseArea()
    {
        var land = CreateLand();

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            4_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.RemovePlot(plot.Id);

        Assert.Empty(land.Plots);
        Assert.Equal(
            0m,
            land.AllocatedPlotAreaInSquareMeters);
        Assert.NotNull(land.UpdatedAt);
    }

    [Fact]
    public void ActivateAndDeactivatePlot_ShouldChangePlotStatus()
    {
        var land = CreateLand();

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            4_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.DeactivatePlot(plot.Id);

        Assert.False(plot.IsActive);
        Assert.NotNull(plot.UpdatedAt);

        land.ActivatePlot(plot.Id);

        Assert.True(plot.IsActive);
    }

    [Fact]
    public void PlotStatus_WhenUnchanged_ShouldNotUpdateParentAgain()
    {
        var land = CreateLand();

        var plot = land.AddPlot(
            "PTK-01",
            "Petak Satu",
            4_000,
            AreaUnit.SquareMeter,
            null,
            null);

        land.DeactivatePlot(plot.Id);

        var parentUpdatedAt = land.UpdatedAt;

        land.DeactivatePlot(plot.Id);

        Assert.Equal(
            parentUpdatedAt,
            land.UpdatedAt);
    }

    [Fact]
    public void UpdatePlot_WhenPlotMissing_ShouldThrow()
    {
        var land = CreateLand();

        Assert.Throws<KeyNotFoundException>(() =>
            land.UpdatePlot(
                Guid.NewGuid(),
                "Petak Tidak Ada",
                1_000,
                AreaUnit.SquareMeter,
                null,
                null));
    }

    [Fact]
    public void ActivateAndDeactivate_ShouldChangeLandStatus()
    {
        var land = CreateLand();

        land.Deactivate();

        Assert.False(land.IsActive);
        Assert.NotNull(land.UpdatedAt);

        land.Activate();

        Assert.True(land.IsActive);
    }

    [Fact]
    public void LandStatus_WhenUnchanged_ShouldNotChangeTimestamp()
    {
        var land = CreateLand();

        land.Activate();

        Assert.Null(land.UpdatedAt);

        land.Deactivate();

        var deactivatedAt = land.UpdatedAt;

        land.Deactivate();

        Assert.Equal(
            deactivatedAt,
            land.UpdatedAt);
    }

    private static Land CreateLand(
        Guid? organizationId = null,
        string code = "LHN-001",
        string name = "Lahan Utama",
        LandTenureType tenureType =
            LandTenureType.Owned,
        decimal totalArea = 1,
        AreaUnit areaUnit =
            AreaUnit.Hectare)
    {
        return Land.Create(
            organizationId ?? Guid.NewGuid(),
            code,
            name,
            tenureType,
            totalArea,
            areaUnit,
            null,
            null,
            null,
            null,
            null);
    }
}
