using SiPacul.Domain.Entities.Cultivation;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Cultivation;

public sealed class CultivationSopTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateSop()
    {
        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();

        var sop = CultivationSop.Create(
            organizationId,
            commodityId,
            "  SOP Budidaya Padi  ",
            "  SOP standar budidaya padi sawah.  ");

        Assert.NotEqual(Guid.Empty, sop.Id);
        Assert.Equal(
            organizationId,
            sop.OrganizationId);
        Assert.Equal(
            commodityId,
            sop.CommodityId);
        Assert.Equal(
            "SOP Budidaya Padi",
            sop.Name);
        Assert.Equal(
            "SOP standar budidaya padi sawah.",
            sop.Description);
        Assert.True(sop.IsActive);
        Assert.Empty(sop.Steps);
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationSop.Create(
                    Guid.Empty,
                    Guid.NewGuid(),
                    "SOP Padi",
                    null));

        Assert.Equal(
            "organizationId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCommodityId_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationSop.Create(
                    Guid.NewGuid(),
                    Guid.Empty,
                    "SOP Padi",
                    null));

        Assert.Equal(
            "commodityId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationSop.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "   ",
                    null));

        Assert.Equal(
            "name",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithNameExceedingMaximum_ShouldThrow()
    {
        var name = new string(
            'A',
            CultivationSop.MaxNameLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationSop.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    name,
                    null));

        Assert.Equal(
            "name",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithWhitespaceDescription_ShouldSetNull()
    {
        var sop = CultivationSop.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SOP Padi",
            "   ");

        Assert.Null(sop.Description);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateSop()
    {
        var sop = CreateSop();

        sop.Update(
            "  SOP Padi Organik  ",
            "  Panduan budidaya organik.  ");

        Assert.Equal(
            "SOP Padi Organik",
            sop.Name);
        Assert.Equal(
            "Panduan budidaya organik.",
            sop.Description);
        Assert.NotNull(sop.UpdatedAt);
    }

    [Fact]
    public void Update_WithUnchangedData_ShouldNotSetUpdatedAt()
    {
        var sop = CultivationSop.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SOP Padi",
            "Panduan budidaya");

        sop.Update(
            "  SOP Padi  ",
            "  Panduan budidaya  ");

        Assert.Null(sop.UpdatedAt);
    }

    [Fact]
    public void AddStep_ShouldAssignSequentialNumbers()
    {
        var sop = CreateSop();

        var firstStep = sop.AddStep(
            "Persiapan Lahan",
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

        Assert.Equal(2, sop.Steps.Count);
        Assert.Equal(1, firstStep.Sequence);
        Assert.Equal(2, secondStep.Sequence);
        Assert.Equal(
            sop.Id,
            firstStep.CultivationSopId);
        Assert.Equal(
            sop.OrganizationId,
            firstStep.OrganizationId);
    }

    [Fact]
    public void AddStep_ShouldNormalizeText()
    {
        var sop = CreateSop();

        var step = sop.AddStep(
            "  Pemupukan Pertama  ",
            "  Gunakan dosis sesuai rekomendasi.  ",
            7,
            1,
            true);

        Assert.Equal(
            "Pemupukan Pertama",
            step.Name);
        Assert.Equal(
            "Gunakan dosis sesuai rekomendasi.",
            step.Description);
    }

    [Fact]
    public void AddStep_WithInvalidDayOffset_ShouldThrow()
    {
        var sop = CreateSop();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sop.AddStep(
                "Tahapan Tidak Valid",
                null,
                CultivationSopStep
                    .MaxPlannedDayOffset + 1,
                1,
                true));
    }

    [Fact]
    public void AddStep_WithInvalidDuration_ShouldThrow()
    {
        var sop = CreateSop();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sop.AddStep(
                "Tahapan Tidak Valid",
                null,
                0,
                0,
                true));
    }

    [Fact]
    public void UpdateStep_WithValidData_ShouldUpdateStep()
    {
        var sop = CreateSop();

        var step = sop.AddStep(
            "Pemupukan",
            null,
            7,
            1,
            true);

        sop.UpdateStep(
            step.Id,
            "  Pemupukan Pertama  ",
            "  Gunakan pupuk dasar.  ",
            10,
            2,
            false);

        Assert.Equal(
            "Pemupukan Pertama",
            step.Name);
        Assert.Equal(
            "Gunakan pupuk dasar.",
            step.Description);
        Assert.Equal(10, step.PlannedDayOffset);
        Assert.Equal(2, step.EstimatedDurationDays);
        Assert.False(step.IsRequired);
        Assert.NotNull(step.UpdatedAt);
    }

    [Fact]
    public void UpdateStep_WhenStepMissing_ShouldThrow()
    {
        var sop = CreateSop();

        Assert.Throws<KeyNotFoundException>(() =>
            sop.UpdateStep(
                Guid.NewGuid(),
                "Pemupukan",
                null,
                7,
                1,
                true));
    }

    [Fact]
    public void RemoveStep_ShouldResequenceRemainingSteps()
    {
        var sop = CreateSop();

        var first = sop.AddStep(
            "Persiapan",
            null,
            -14,
            7,
            true);

        var second = sop.AddStep(
            "Penanaman",
            null,
            0,
            1,
            true);

        var third = sop.AddStep(
            "Pemupukan",
            null,
            7,
            1,
            true);

        sop.RemoveStep(second.Id);

        Assert.Equal(2, sop.Steps.Count);
        Assert.Equal(first.Id, sop.Steps.ElementAt(0).Id);
        Assert.Equal(1, sop.Steps.ElementAt(0).Sequence);
        Assert.Equal(third.Id, sop.Steps.ElementAt(1).Id);
        Assert.Equal(2, sop.Steps.ElementAt(1).Sequence);
    }

    [Fact]
    public void MoveStep_ShouldReorderAndResequenceSteps()
    {
        var sop = CreateSop();

        var first = sop.AddStep(
            "Persiapan",
            null,
            -14,
            7,
            true);

        var second = sop.AddStep(
            "Penanaman",
            null,
            0,
            1,
            true);

        var third = sop.AddStep(
            "Pemupukan",
            null,
            7,
            1,
            true);

        sop.MoveStep(
            third.Id,
            1);

        Assert.Equal(third.Id, sop.Steps.ElementAt(0).Id);
        Assert.Equal(1, sop.Steps.ElementAt(0).Sequence);
        Assert.Equal(first.Id, sop.Steps.ElementAt(1).Id);
        Assert.Equal(2, sop.Steps.ElementAt(1).Sequence);
        Assert.Equal(second.Id, sop.Steps.ElementAt(2).Id);
        Assert.Equal(3, sop.Steps.ElementAt(2).Sequence);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeStatus()
    {
        var sop = CreateSop();

        sop.Deactivate();

        Assert.False(sop.IsActive);
        Assert.NotNull(sop.UpdatedAt);

        sop.Activate();

        Assert.True(sop.IsActive);
    }

    private static CultivationSop CreateSop()
    {
        return CultivationSop.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SOP Budidaya Padi",
            null);
    }
}
