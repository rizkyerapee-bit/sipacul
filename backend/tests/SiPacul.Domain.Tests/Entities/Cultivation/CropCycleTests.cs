using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Cultivation;

public sealed class CropCycleTests
{
    private static readonly DateOnly PlannedStartDate =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvestDate =
        new(2027, 5, 10);

    [Fact]
    public void Create_WithValidData_ShouldCreatePlannedCycle()
    {
        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();
        var cultivationSopId = Guid.NewGuid();
        var landId = Guid.NewGuid();
        var plotId = Guid.NewGuid();

        var cropCycle = CropCycle.Create(
            organizationId,
            "  sc-padi-2027-01  ",
            "  Musim Tanam Padi Pertama  ",
            commodityId,
            cultivationSopId,
            landId,
            plotId,
            0.75m,
            AreaUnit.Hectare,
            PlannedStartDate,
            ExpectedHarvestDate,
            "  Fokus pada varietas unggul  ");

        Assert.NotEqual(Guid.Empty, cropCycle.Id);
        Assert.Equal(
            organizationId,
            cropCycle.OrganizationId);
        Assert.Equal(
            "SC-PADI-2027-01",
            cropCycle.Code);
        Assert.Equal(
            "Musim Tanam Padi Pertama",
            cropCycle.Name);
        Assert.Equal(
            commodityId,
            cropCycle.CommodityId);
        Assert.Equal(
            cultivationSopId,
            cropCycle.CultivationSopId);
        Assert.Equal(landId, cropCycle.LandId);
        Assert.Equal(plotId, cropCycle.LandPlotId);
        Assert.Equal(0.75m, cropCycle.PlantedArea);
        Assert.Equal(
            AreaUnit.Hectare,
            cropCycle.AreaUnit);
        Assert.Equal(
            7_500m,
            cropCycle.PlantedAreaInSquareMeters);
        Assert.Equal(
            PlannedStartDate,
            cropCycle.PlannedStartDate);
        Assert.Equal(
            ExpectedHarvestDate,
            cropCycle.ExpectedHarvestDate);
        Assert.Equal(
            CropCycleStatus.Planned,
            cropCycle.Status);
        Assert.Null(cropCycle.ActualStartDate);
        Assert.Null(cropCycle.ActualHarvestDate);
        Assert.Null(cropCycle.CancellationReason);
        Assert.Equal(
            "Fokus pada varietas unggul",
            cropCycle.Notes);
        Assert.False(cropCycle.IsDeleted);
        Assert.Null(cropCycle.UpdatedAt);
    }

    [Fact]
    public void Create_WithoutSop_ShouldBeAllowed()
    {
        var cropCycle = CreateCropCycle(
            cultivationSopId: null);

        Assert.Null(cropCycle.CultivationSopId);
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(
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
                CreateCropCycle(code: invalidCode));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("SC PADI 01")]
    [InlineData("SC/PADI/01")]
    [InlineData("SC.PADI.01")]
    public void Create_WithInvalidCode_ShouldThrow(
        string invalidCode)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(code: invalidCode));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_WithCodeExceedingMaximum_ShouldThrow()
    {
        var invalidCode = new string(
            'A',
            CropCycle.MaxCodeLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(code: invalidCode));

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
                CreateCropCycle(name: invalidName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithNameExceedingMaximum_ShouldThrow()
    {
        var invalidName = new string(
            'A',
            CropCycle.MaxNameLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(name: invalidName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCommodityId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(
                    commodityId: Guid.Empty));

        Assert.Equal(
            "commodityId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCultivationSopId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(
                    cultivationSopId: Guid.Empty));

        Assert.Equal(
            "cultivationSopId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyLandId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(
                    landId: Guid.Empty));

        Assert.Equal("landId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyPlotId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(
                    plotId: Guid.Empty));

        Assert.Equal(
            "landPlotId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveArea_ShouldThrow(
        decimal invalidArea)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCropCycle(
                    plantedArea: invalidArea));

        Assert.Equal(
            "plantedArea",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedAreaUnit_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCropCycle(
                    areaUnit: (AreaUnit)999));

        Assert.Equal("unit", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidHarvestDate_ShouldThrow(
        int harvestDayOffset)
    {
        var expectedHarvestDate =
            PlannedStartDate.AddDays(
                harvestDayOffset);

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCropCycle(
                    expectedHarvestDate:
                        expectedHarvestDate));

        Assert.Equal(
            "expectedHarvestDate",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithWhitespaceOptionalText_ShouldSetNull()
    {
        var cropCycle = CropCycle.Create(
            Guid.NewGuid(),
            "SC-PADI-001",
            "Musim Tanam Padi",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            5_000,
            AreaUnit.SquareMeter,
            PlannedStartDate,
            ExpectedHarvestDate,
            "   ");

        Assert.Null(cropCycle.Notes);
    }

    [Fact]
    public void Create_WithNotesExceedingMaximum_ShouldThrow()
    {
        var invalidNotes = new string(
            'A',
            CropCycle.MaxNotesLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CreateCropCycle(notes: invalidNotes));

        Assert.Equal("notes", exception.ParamName);
    }

    [Fact]
    public void UpdatePlan_WithValidData_ShouldUpdatePlan()
    {
        var cropCycle = CreateCropCycle(
            cultivationSopId: null);

        var cultivationSopId = Guid.NewGuid();
        var newStartDate =
            PlannedStartDate.AddDays(7);
        var newHarvestDate =
            ExpectedHarvestDate.AddDays(14);

        cropCycle.UpdatePlan(
            "  Musim Tanam Padi Organik  ",
            cultivationSopId,
            8_000,
            AreaUnit.SquareMeter,
            newStartDate,
            newHarvestDate,
            "  Mengikuti standar organik  ");

        Assert.Equal(
            "Musim Tanam Padi Organik",
            cropCycle.Name);
        Assert.Equal(
            cultivationSopId,
            cropCycle.CultivationSopId);
        Assert.Equal(8_000m, cropCycle.PlantedArea);
        Assert.Equal(
            AreaUnit.SquareMeter,
            cropCycle.AreaUnit);
        Assert.Equal(
            newStartDate,
            cropCycle.PlannedStartDate);
        Assert.Equal(
            newHarvestDate,
            cropCycle.ExpectedHarvestDate);
        Assert.Equal(
            "Mengikuti standar organik",
            cropCycle.Notes);
        Assert.NotNull(cropCycle.UpdatedAt);
    }

    [Fact]
    public void UpdatePlan_WithUnchangedData_ShouldNotSetUpdatedAt()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.UpdatePlan(
            "  Musim Tanam Padi  ",
            null,
            1,
            AreaUnit.Hectare,
            PlannedStartDate,
            ExpectedHarvestDate,
            "   ");

        Assert.Null(cropCycle.UpdatedAt);
    }

    [Fact]
    public void UpdatePlan_AfterStart_ShouldThrowWithoutMutation()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.UpdatePlan(
                "Nama Baru",
                Guid.NewGuid(),
                5_000,
                AreaUnit.SquareMeter,
                PlannedStartDate,
                ExpectedHarvestDate,
                null));

        Assert.Equal(
            "Musim Tanam Padi",
            cropCycle.Name);
        Assert.Equal(1m, cropCycle.PlantedArea);
    }

    [Fact]
    public void Start_FromPlanned_ShouldSetInProgress()
    {
        var cropCycle = CreateCropCycle();

        var actualStartDate =
            PlannedStartDate.AddDays(2);

        cropCycle.Start(actualStartDate);

        Assert.Equal(
            CropCycleStatus.InProgress,
            cropCycle.Status);
        Assert.Equal(
            actualStartDate,
            cropCycle.ActualStartDate);
        Assert.Null(cropCycle.ActualHarvestDate);
        Assert.NotNull(cropCycle.UpdatedAt);
    }

    [Fact]
    public void Start_BeforePlannedDate_ShouldBeAllowed()
    {
        var cropCycle = CreateCropCycle();

        var actualStartDate =
            PlannedStartDate.AddDays(-3);

        cropCycle.Start(actualStartDate);

        Assert.Equal(
            actualStartDate,
            cropCycle.ActualStartDate);
        Assert.Equal(
            CropCycleStatus.InProgress,
            cropCycle.Status);
    }

    [Fact]
    public void Start_AfterExpectedHarvestDate_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cropCycle.Start(
                    ExpectedHarvestDate.AddDays(1)));

        Assert.Equal(
            "actualStartDate",
            exception.ParamName);
        Assert.Equal(
            CropCycleStatus.Planned,
            cropCycle.Status);
        Assert.Null(cropCycle.ActualStartDate);
    }

    [Fact]
    public void Start_WhenAlreadyInProgress_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.Start(
                PlannedStartDate.AddDays(1)));
    }

    [Fact]
    public void Complete_FromInProgress_ShouldSetCompleted()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);

        var actualHarvestDate =
            ExpectedHarvestDate.AddDays(3);

        cropCycle.Complete(actualHarvestDate);

        Assert.Equal(
            CropCycleStatus.Completed,
            cropCycle.Status);
        Assert.Equal(
            actualHarvestDate,
            cropCycle.ActualHarvestDate);
        Assert.NotNull(cropCycle.UpdatedAt);
    }

    [Fact]
    public void Complete_OnActualStartDate_ShouldBeAllowed()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);
        cropCycle.Complete(PlannedStartDate);

        Assert.Equal(
            CropCycleStatus.Completed,
            cropCycle.Status);
        Assert.Equal(
            PlannedStartDate,
            cropCycle.ActualHarvestDate);
    }

    [Fact]
    public void Complete_BeforeActualStartDate_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(
            PlannedStartDate.AddDays(2));

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cropCycle.Complete(
                    PlannedStartDate.AddDays(1)));

        Assert.Equal(
            "actualHarvestDate",
            exception.ParamName);
        Assert.Equal(
            CropCycleStatus.InProgress,
            cropCycle.Status);
        Assert.Null(cropCycle.ActualHarvestDate);
    }

    [Fact]
    public void Complete_FromPlanned_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.Complete(
                ExpectedHarvestDate));

        Assert.Equal(
            CropCycleStatus.Planned,
            cropCycle.Status);
    }

    [Fact]
    public void Cancel_FromPlanned_ShouldSetCancelled()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Cancel(
            "  Perubahan rencana investasi  ");

        Assert.Equal(
            CropCycleStatus.Cancelled,
            cropCycle.Status);
        Assert.Equal(
            "Perubahan rencana investasi",
            cropCycle.CancellationReason);
        Assert.Null(cropCycle.ActualStartDate);
        Assert.Null(cropCycle.ActualHarvestDate);
        Assert.NotNull(cropCycle.UpdatedAt);
    }

    [Fact]
    public void Cancel_FromInProgress_ShouldPreserveStartDate()
    {
        var cropCycle = CreateCropCycle();

        var actualStartDate =
            PlannedStartDate.AddDays(1);

        cropCycle.Start(actualStartDate);
        cropCycle.Cancel("Serangan hama berat");

        Assert.Equal(
            CropCycleStatus.Cancelled,
            cropCycle.Status);
        Assert.Equal(
            actualStartDate,
            cropCycle.ActualStartDate);
        Assert.Null(cropCycle.ActualHarvestDate);
        Assert.Equal(
            "Serangan hama berat",
            cropCycle.CancellationReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Cancel_WithEmptyReason_ShouldThrow(
        string invalidReason)
    {
        var cropCycle = CreateCropCycle();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                cropCycle.Cancel(invalidReason));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
        Assert.Equal(
            CropCycleStatus.Planned,
            cropCycle.Status);
    }

    [Fact]
    public void Cancel_WithReasonExceedingMaximum_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        var invalidReason = new string(
            'A',
            CropCycle.MaxCancellationReasonLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                cropCycle.Cancel(invalidReason));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_FromCompleted_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);
        cropCycle.Complete(ExpectedHarvestDate);

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.Cancel("Tidak berlaku"));
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Cancel("Rencana dibatalkan");

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.Cancel("Alasan baru"));
    }

    [Fact]
    public void UpdateNotes_WhilePlanned_ShouldUpdateNotes()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.UpdateNotes(
            "  Perlu pengecekan saluran air  ");

        Assert.Equal(
            "Perlu pengecekan saluran air",
            cropCycle.Notes);
        Assert.NotNull(cropCycle.UpdatedAt);
    }

    [Fact]
    public void UpdateNotes_WhileInProgress_ShouldUpdateNotes()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);
        cropCycle.UpdateNotes(
            "Pertumbuhan minggu pertama baik");

        Assert.Equal(
            "Pertumbuhan minggu pertama baik",
            cropCycle.Notes);
    }

    [Fact]
    public void UpdateNotes_WithSameValue_ShouldNotUpdateAgain()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.UpdateNotes("Catatan awal");

        var firstUpdatedAt =
            cropCycle.UpdatedAt;

        cropCycle.UpdateNotes(
            "  Catatan awal  ");

        Assert.Equal(
            firstUpdatedAt,
            cropCycle.UpdatedAt);
    }

    [Fact]
    public void UpdateNotes_WithWhitespace_ShouldSetNull()
    {
        var cropCycle = CreateCropCycle(
            notes: "Catatan awal");

        cropCycle.UpdateNotes("   ");

        Assert.Null(cropCycle.Notes);
    }

    [Fact]
    public void UpdateNotes_AfterCompletion_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Start(PlannedStartDate);
        cropCycle.Complete(ExpectedHarvestDate);

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.UpdateNotes(
                "Tidak boleh berubah"));
    }

    [Fact]
    public void UpdateNotes_AfterCancellation_ShouldThrow()
    {
        var cropCycle = CreateCropCycle();

        cropCycle.Cancel("Rencana dibatalkan");

        Assert.Throws<InvalidOperationException>(() =>
            cropCycle.UpdateNotes(
                "Tidak boleh berubah"));
    }

    private static CropCycle CreateCropCycle(
        Guid? organizationId = null,
        string code = "SC-PADI-001",
        string name = "Musim Tanam Padi",
        Guid? commodityId = null,
        Guid? cultivationSopId = null,
        Guid? landId = null,
        Guid? plotId = null,
        decimal plantedArea = 1,
        AreaUnit areaUnit = AreaUnit.Hectare,
        DateOnly? plannedStartDate = null,
        DateOnly? expectedHarvestDate = null,
        string? notes = null)
    {
        return CropCycle.Create(
            organizationId ?? Guid.NewGuid(),
            code,
            name,
            commodityId ?? Guid.NewGuid(),
            cultivationSopId,
            landId ?? Guid.NewGuid(),
            plotId ?? Guid.NewGuid(),
            plantedArea,
            areaUnit,
            plannedStartDate ?? PlannedStartDate,
            expectedHarvestDate ??
                ExpectedHarvestDate,
            notes);
    }
}
