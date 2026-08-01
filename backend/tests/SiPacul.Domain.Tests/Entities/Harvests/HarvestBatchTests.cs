using SiPacul.Domain.Entities.Harvests;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Harvests;

public sealed class HarvestBatchTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly DateOnly HarvestDate =
        new(2027, 5, 1);

    [Fact]
    public void Create_WithValidValues_ShouldCreateDraft()
    {
        var batch = CreateBatch();

        Assert.NotEqual(Guid.Empty, batch.Id);
        Assert.Equal(OrganizationId, batch.OrganizationId);
        Assert.Equal(CropCycleId, batch.CropCycleId);
        Assert.Equal("HRV-001", batch.Code);
        Assert.Equal(HarvestDate, batch.HarvestDate);
        Assert.Equal(1000m, batch.GrossQuantity);
        Assert.Equal(25m, batch.RejectedQuantity);
        Assert.Equal(975m, batch.NetQuantity);

        Assert.Equal(
            HarvestQuantityUnit.Kilogram,
            batch.QuantityUnit);

        Assert.Equal("Grade A", batch.QualityGrade);
        Assert.Equal("Gudang Utama", batch.StorageLocation);
        Assert.Equal("Panen pagi", batch.Notes);

        Assert.Equal(
            HarvestBatchStatus.Draft,
            batch.Status);

        Assert.Null(batch.ConfirmedAt);
        Assert.Null(batch.CancellationReason);
        Assert.False(batch.IsSellable);
    }

    [Fact]
    public void Create_ShouldNormalizeCodeAndOptionalText()
    {
        var batch =
            HarvestBatch.Create(
                OrganizationId,
                CropCycleId,
                "  hrv.abc_01-x  ",
                HarvestDate,
                100,
                5,
                HarvestQuantityUnit.Kilogram,
                "  Premium  ",
                "  Gudang Barat  ",
                "  Dipanen manual  ");

        Assert.Equal("HRV.ABC_01-X", batch.Code);
        Assert.Equal("Premium", batch.QualityGrade);
        Assert.Equal("Gudang Barat", batch.StorageLocation);
        Assert.Equal("Dipanen manual", batch.Notes);
    }

    [Fact]
    public void Create_WithBlankOptionalText_ShouldUseNull()
    {
        var batch =
            HarvestBatch.Create(
                OrganizationId,
                CropCycleId,
                "HRV-NULL",
                HarvestDate,
                10,
                0,
                HarvestQuantityUnit.Kilogram,
                " ",
                null,
                "");

        Assert.Null(batch.QualityGrade);
        Assert.Null(batch.StorageLocation);
        Assert.Null(batch.Notes);
    }

    [Fact]
    public void Create_ShouldRoundQuantitiesAwayFromZero()
    {
        var batch =
            HarvestBatch.Create(
                OrganizationId,
                CropCycleId,
                "HRV-ROUND",
                HarvestDate,
                10.12345m,
                0.12345m,
                HarvestQuantityUnit.Kilogram,
                null,
                null,
                null);

        Assert.Equal(10.1235m, batch.GrossQuantity);
        Assert.Equal(0.1235m, batch.RejectedQuantity);
        Assert.Equal(10.0000m, batch.NetQuantity);
    }

    [Fact]
    public void Create_WithAllRejectedQuantity_ShouldAllowDraft()
    {
        var batch =
            HarvestBatch.Create(
                OrganizationId,
                CropCycleId,
                "HRV-ZERO-NET",
                HarvestDate,
                10,
                10,
                HarvestQuantityUnit.Kilogram,
                null,
                null,
                null);

        Assert.Equal(0m, batch.NetQuantity);

        Assert.Equal(
            HarvestBatchStatus.Draft,
            batch.Status);
    }

    [Theory]
    [InlineData("", "code")]
    [InlineData(" ", "code")]
    [InlineData("-HRV", "code")]
    [InlineData("_HRV", "code")]
    [InlineData(".HRV", "code")]
    [InlineData("HRV/001", "code")]
    [InlineData("HRV 001", "code")]
    [InlineData("HRV@001", "code")]
    public void Create_WithInvalidCode_ShouldThrow(
        string code,
        string parameterName)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    code,
                    HarvestDate,
                    10,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            parameterName,
            exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongCode_ShouldThrow()
    {
        var code = "A" + new string('B', 40);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    code,
                    HarvestDate,
                    10,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(true, false, "organizationId")]
    [InlineData(false, true, "cropCycleId")]
    public void Create_WithEmptyIdentifier_ShouldThrow(
        bool emptyOrganization,
        bool emptyCropCycle,
        string parameterName)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                HarvestBatch.Create(
                    emptyOrganization
                        ? Guid.Empty
                        : OrganizationId,
                    emptyCropCycle
                        ? Guid.Empty
                        : CropCycleId,
                    "HRV-ID",
                    HarvestDate,
                    10,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            parameterName,
            exception.ParamName);
    }

    [Fact]
    public void Create_WithDefaultDate_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    "HRV-DATE",
                    default,
                    10,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            "harvestDate",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.00001)]
    public void Create_WithNonPositiveRoundedGross_ShouldThrow(
        double gross)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    "HRV-GROSS",
                    HarvestDate,
                    (decimal)gross,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            "grossQuantity",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithNegativeRejected_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    "HRV-REJECT",
                    HarvestDate,
                    10,
                    -0.1m,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            "rejectedQuantity",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithRejectedGreaterThanGross_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    "HRV-REJECT-MAX",
                    HarvestDate,
                    10,
                    10.0001m,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            "rejectedQuantity",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedUnit_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    "HRV-UNIT",
                    HarvestDate,
                    10,
                    0,
                    (HarvestQuantityUnit)999,
                    null,
                    null,
                    null));

        Assert.Equal(
            "quantityUnit",
            exception.ParamName);
    }

    [Theory]
    [InlineData("qualityGrade", 101)]
    [InlineData("storageLocation", 251)]
    [InlineData("notes", 1001)]
    public void Create_WithTooLongOptionalText_ShouldThrow(
        string field,
        int length)
    {
        var value = new string('X', length);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                HarvestBatch.Create(
                    OrganizationId,
                    CropCycleId,
                    "HRV-TEXT",
                    HarvestDate,
                    10,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    field == "qualityGrade"
                        ? value
                        : null,
                    field == "storageLocation"
                        ? value
                        : null,
                    field == "notes"
                        ? value
                        : null));

        Assert.Equal(field, exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithValidValues_ShouldUpdate()
    {
        var batch = CreateBatch();

        batch.UpdateDraft(
            new DateOnly(2027, 5, 2),
            1200.12345m,
            20.12345m,
            HarvestQuantityUnit.Quintal,
            "  Grade B  ",
            "  Gudang Timur  ",
            "  Panen siang  ");

        Assert.Equal(
            new DateOnly(2027, 5, 2),
            batch.HarvestDate);

        Assert.Equal(1200.1235m, batch.GrossQuantity);
        Assert.Equal(20.1235m, batch.RejectedQuantity);
        Assert.Equal(1180.0000m, batch.NetQuantity);

        Assert.Equal(
            HarvestQuantityUnit.Quintal,
            batch.QuantityUnit);

        Assert.Equal("Grade B", batch.QualityGrade);
        Assert.Equal("Gudang Timur", batch.StorageLocation);
        Assert.Equal("Panen siang", batch.Notes);
        Assert.NotNull(batch.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_ShouldNotChangeCodeOrOwnership()
    {
        var batch = CreateBatch();

        batch.UpdateDraft(
            new DateOnly(2027, 5, 2),
            900,
            10,
            HarvestQuantityUnit.Kilogram,
            null,
            null,
            null);

        Assert.Equal("HRV-001", batch.Code);
        Assert.Equal(OrganizationId, batch.OrganizationId);
        Assert.Equal(CropCycleId, batch.CropCycleId);
    }

    [Fact]
    public void UpdateDraft_WithDefaultDate_ShouldThrow()
    {
        var batch = CreateBatch();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                batch.UpdateDraft(
                    default,
                    10,
                    0,
                    HarvestQuantityUnit.Kilogram,
                    null,
                    null,
                    null));

        Assert.Equal(
            "harvestDate",
            exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithInvalidQuantity_ShouldThrow()
    {
        var batch = CreateBatch();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            batch.UpdateDraft(
                HarvestDate,
                10,
                11,
                HarvestQuantityUnit.Kilogram,
                null,
                null,
                null));
    }

    [Fact]
    public void Confirm_WithPositiveNet_ShouldConfirm()
    {
        var batch = CreateBatch();

        var before = DateTime.UtcNow;

        batch.Confirm();

        var after = DateTime.UtcNow;

        Assert.Equal(
            HarvestBatchStatus.Confirmed,
            batch.Status);

        Assert.True(batch.IsSellable);
        Assert.NotNull(batch.ConfirmedAt);

        Assert.InRange(
            batch.ConfirmedAt!.Value,
            before,
            after);

        Assert.NotNull(batch.UpdatedAt);
    }

    [Fact]
    public void Confirm_WithZeroNet_ShouldThrow()
    {
        var batch =
            HarvestBatch.Create(
                OrganizationId,
                CropCycleId,
                "HRV-ZERO",
                HarvestDate,
                10,
                10,
                HarvestQuantityUnit.Kilogram,
                null,
                null,
                null);

        Assert.Throws<InvalidOperationException>(
            batch.Confirm);

        Assert.Equal(
            HarvestBatchStatus.Draft,
            batch.Status);

        Assert.Null(batch.ConfirmedAt);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrow()
    {
        var batch = CreateConfirmedBatch();

        Assert.Throws<InvalidOperationException>(
            batch.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenConfirmed_ShouldThrow()
    {
        var batch = CreateConfirmedBatch();

        Assert.Throws<InvalidOperationException>(() =>
            batch.UpdateDraft(
                HarvestDate,
                900,
                10,
                HarvestQuantityUnit.Kilogram,
                null,
                null,
                null));
    }

    [Fact]
    public void Cancel_FromDraft_ShouldCancel()
    {
        var batch = CreateBatch();

        batch.Cancel("  Hasil tidak valid  ");

        Assert.Equal(
            HarvestBatchStatus.Cancelled,
            batch.Status);

        Assert.Equal(
            "Hasil tidak valid",
            batch.CancellationReason);

        Assert.False(batch.IsSellable);
        Assert.NotNull(batch.UpdatedAt);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldCancel()
    {
        var batch = CreateConfirmedBatch();

        var confirmedAt = batch.ConfirmedAt;

        batch.Cancel("Penjualan dibatalkan");

        Assert.Equal(
            HarvestBatchStatus.Cancelled,
            batch.Status);

        Assert.Equal(
            "Penjualan dibatalkan",
            batch.CancellationReason);

        Assert.Equal(confirmedAt, batch.ConfirmedAt);
        Assert.False(batch.IsSellable);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Cancel_WithBlankReason_ShouldThrow(
        string reason)
    {
        var batch = CreateBatch();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                batch.Cancel(reason));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WithTooLongReason_ShouldThrow()
    {
        var batch = CreateBatch();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                batch.Cancel(
                    new string(
                        'X',
                        HarvestBatch
                            .MaxCancellationReasonLength +
                        1)));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var batch = CreateBatch();

        batch.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            batch.Cancel("Batal lagi"));
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrow()
    {
        var batch = CreateBatch();

        batch.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(
            batch.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenCancelled_ShouldThrow()
    {
        var batch = CreateBatch();

        batch.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            batch.UpdateDraft(
                HarvestDate,
                900,
                10,
                HarvestQuantityUnit.Kilogram,
                null,
                null,
                null));
    }

    [Theory]
    [InlineData(HarvestQuantityUnit.Kilogram)]
    [InlineData(HarvestQuantityUnit.Ton)]
    [InlineData(HarvestQuantityUnit.Quintal)]
    [InlineData(HarvestQuantityUnit.Piece)]
    [InlineData(HarvestQuantityUnit.Bunch)]
    [InlineData(HarvestQuantityUnit.Sack)]
    [InlineData(HarvestQuantityUnit.Crate)]
    [InlineData(HarvestQuantityUnit.Liter)]
    public void Create_WithSupportedUnit_ShouldPreserveUnit(
        HarvestQuantityUnit unit)
    {
        var batch =
            HarvestBatch.Create(
                OrganizationId,
                CropCycleId,
                $"HRV-{(int)unit}",
                HarvestDate,
                10,
                0,
                unit,
                null,
                null,
                null);

        Assert.Equal(unit, batch.QuantityUnit);
    }

    private static HarvestBatch CreateBatch()
    {
        return HarvestBatch.Create(
            OrganizationId,
            CropCycleId,
            "  hrv-001  ",
            HarvestDate,
            1000,
            25,
            HarvestQuantityUnit.Kilogram,
            "  Grade A  ",
            "  Gudang Utama  ",
            "  Panen pagi  ");
    }

    private static HarvestBatch CreateConfirmedBatch()
    {
        var batch = CreateBatch();
        batch.Confirm();

        return batch;
    }
}
