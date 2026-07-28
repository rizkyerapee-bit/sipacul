using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Domain.Tests.Entities.MasterData;

public sealed class CommodityCategoryTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCommodityCategory()
    {
        var category = CommodityCategory.Create(
            "Tanaman Pangan",
            "Komoditas pangan utama");

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Tanaman Pangan", category.Name);
        Assert.Equal(
            "Komoditas pangan utama",
            category.Description);
        Assert.True(category.IsActive);
        Assert.False(category.IsDeleted);
        Assert.Null(category.UpdatedAt);
        Assert.Empty(category.Commodities);
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        var category = CommodityCategory.Create(
            "  Hortikultura  ",
            "  Sayuran dan buah  ");

        Assert.Equal("Hortikultura", category.Name);
        Assert.Equal(
            "Sayuran dan buah",
            category.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowArgumentException(
        string invalidName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CommodityCategory.Create(
                invalidName,
                null));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithNameExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var invalidName = new string(
            'A',
            CommodityCategory.MaxNameLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            CommodityCategory.Create(
                invalidName,
                null));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithDescriptionExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var invalidDescription = new string(
            'A',
            CommodityCategory.MaxDescriptionLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            CommodityCategory.Create(
                "Perkebunan",
                invalidDescription));

        Assert.Equal(
            "description",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithNullDescription_ShouldCreateCommodityCategory()
    {
        var category = CommodityCategory.Create(
            "Peternakan",
            null);

        Assert.Null(category.Description);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateCommodityCategory()
    {
        var category = CommodityCategory.Create(
            "Tanaman",
            "Deskripsi awal");

        category.Update(
            "Tanaman Perkebunan",
            "Deskripsi baru");

        Assert.Equal(
            "Tanaman Perkebunan",
            category.Name);
        Assert.Equal(
            "Deskripsi baru",
            category.Description);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldTrimNameAndDescription()
    {
        var category = CommodityCategory.Create(
            "Tanaman",
            null);

        category.Update(
            "  Perikanan  ",
            "  Budidaya perairan  ");

        Assert.Equal("Perikanan", category.Name);
        Assert.Equal(
            "Budidaya perairan",
            category.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_ShouldThrowArgumentException(
        string invalidName)
    {
        var category = CommodityCategory.Create(
            "Tanaman",
            null);

        var exception = Assert.Throws<ArgumentException>(() =>
            category.Update(
                invalidName,
                null));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateCommodityCategory()
    {
        var category = CommodityCategory.Create(
            "Tanaman Pangan",
            null);

        category.Deactivate();

        Assert.False(category.IsActive);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenInactive_ShouldActivateCommodityCategory()
    {
        var category = CommodityCategory.Create(
            "Tanaman Pangan",
            null);

        category.Deactivate();
        category.Activate();

        Assert.True(category.IsActive);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void ActivateAndDeactivate_WhenStateIsUnchanged_ShouldNotUpdateTimestamp()
    {
        var category = CommodityCategory.Create(
            "Tanaman Pangan",
            null);

        category.Activate();

        Assert.Null(category.UpdatedAt);

        category.Deactivate();

        var deactivatedAt = category.UpdatedAt;

        category.Deactivate();

        Assert.Equal(
            deactivatedAt,
            category.UpdatedAt);

        category.Activate();

        var activatedAt = category.UpdatedAt;

        category.Activate();

        Assert.Equal(
            activatedAt,
            category.UpdatedAt);
    }
}
