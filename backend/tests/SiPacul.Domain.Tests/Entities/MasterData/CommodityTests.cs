using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Events.MasterData;

namespace SiPacul.Domain.Tests.Entities.MasterData;

public sealed class CommodityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCommodity()
    {
        var organizationId = Guid.NewGuid();
        var code = CommodityCode.Create("PADI");
        var categoryId = Guid.NewGuid();

        var commodity = Commodity.Create(
            organizationId,
            code,
            "Padi",
            categoryId,
            "Oryza sativa",
            "Tanaman pangan utama.");

        Assert.NotEqual(Guid.Empty, commodity.Id);
        Assert.Equal(
            organizationId,
            commodity.OrganizationId);
        Assert.Equal(code, commodity.Code);
        Assert.Equal("Padi", commodity.Name);
        Assert.Equal(
            categoryId,
            commodity.CommodityCategoryId);
        Assert.Equal(
            "Oryza sativa",
            commodity.ScientificName);
        Assert.Equal(
            "Tanaman pangan utama.",
            commodity.Description);
        Assert.True(commodity.IsActive);
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Commodity.Create(
                Guid.Empty,
                CommodityCode.Create("PADI"),
                "Padi",
                Guid.NewGuid(),
                null,
                null));

        Assert.Equal(
            "organizationId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCategoryId_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Commodity.Create(
                Guid.NewGuid(),
                CommodityCode.Create("PADI"),
                "Padi",
                Guid.Empty,
                null,
                null));

        Assert.Equal(
            "commodityCategoryId",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithWhitespaceAroundText_ShouldTrimText()
    {
        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("CABAI"),
            "  Cabai Merah  ",
            Guid.NewGuid(),
            "  Capsicum annuum  ",
            "  Komoditas hortikultura.  ");

        Assert.Equal("Cabai Merah", commodity.Name);
        Assert.Equal(
            "Capsicum annuum",
            commodity.ScientificName);
        Assert.Equal(
            "Komoditas hortikultura.",
            commodity.Description);
    }

    [Fact]
    public void Create_WithWhitespaceOptionalText_ShouldSetValuesToNull()
    {
        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("CABAI"),
            "Cabai",
            Guid.NewGuid(),
            "   ",
            "   ");

        Assert.Null(commodity.ScientificName);
        Assert.Null(commodity.Description);
    }

    [Fact]
    public void Create_WithValidData_ShouldRaiseCommodityCreatedDomainEvent()
    {
        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("JAGUNG"),
            "Jagung",
            Guid.NewGuid(),
            "Zea mays",
            null);

        Assert.Single(commodity.DomainEvents);
        Assert.IsType<CommodityCreatedDomainEvent>(
            commodity.DomainEvents.Single());
    }

    [Fact]
    public void Create_ShouldRaiseDomainEventWithCorrectData()
    {
        var categoryId = Guid.NewGuid();

        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("KEDELAI"),
            "Kedelai",
            categoryId,
            "Glycine max",
            null);

        var domainEvent = Assert.IsType<CommodityCreatedDomainEvent>(
            commodity.DomainEvents.Single());

        Assert.Equal(
            commodity.Id,
            domainEvent.CommodityId);
        Assert.Equal(
            commodity.Name,
            domainEvent.CommodityName);
        Assert.Equal(
            categoryId,
            domainEvent.CommodityCategoryId);
    }

    [Fact]
    public void ClearDomainEvents_AfterCommodityCreated_ShouldRemoveAllEvents()
    {
        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("TOMAT"),
            "Tomat",
            Guid.NewGuid(),
            "Solanum lycopersicum",
            null);

        Assert.Single(commodity.DomainEvents);

        commodity.ClearDomainEvents();

        Assert.Empty(commodity.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowArgumentException(
        string name)
    {
        var action = () => Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("INVALID"),
            name,
            Guid.NewGuid(),
            null,
            null);

        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            "Commodity name cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateCommodity()
    {
        var organizationId = Guid.NewGuid();

        var commodity = Commodity.Create(
            organizationId,
            CommodityCode.Create("CABAI"),
            "Cabai",
            Guid.NewGuid(),
            null,
            null);

        var newCategoryId = Guid.NewGuid();

        commodity.Update(
            "  Cabai Merah  ",
            newCategoryId,
            "  Capsicum annuum  ",
            "  Cabai untuk produksi komersial  ");

        Assert.Equal(
            organizationId,
            commodity.OrganizationId);
        Assert.Equal("Cabai Merah", commodity.Name);
        Assert.Equal(
            newCategoryId,
            commodity.CommodityCategoryId);
        Assert.Equal(
            "Capsicum annuum",
            commodity.ScientificName);
        Assert.Equal(
            "Cabai untuk produksi komersial",
            commodity.Description);
        Assert.NotNull(commodity.UpdatedAt);
    }

    [Fact]
    public void Update_WithEmptyCategoryId_ShouldThrowArgumentException()
    {
        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("CABAI"),
            "Cabai",
            Guid.NewGuid(),
            null,
            null);

        var exception = Assert.Throws<ArgumentException>(() =>
            commodity.Update(
                "Cabai Merah",
                Guid.Empty,
                null,
                null));

        Assert.Equal(
            "commodityCategoryId",
            exception.ParamName);
    }

    [Fact]
    public void Update_WithUnchangedData_ShouldNotSetUpdatedAt()
    {
        var categoryId = Guid.NewGuid();

        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("CABAI"),
            "Cabai Merah",
            categoryId,
            "Capsicum annuum",
            "Komoditas hortikultura");

        commodity.Update(
            "  Cabai Merah  ",
            categoryId,
            "  Capsicum annuum  ",
            "  Komoditas hortikultura  ");

        Assert.Null(commodity.UpdatedAt);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeStatus()
    {
        var commodity = Commodity.Create(
            Guid.NewGuid(),
            CommodityCode.Create("NANAS"),
            "Nanas",
            Guid.NewGuid(),
            null,
            null);

        commodity.Deactivate();

        Assert.False(commodity.IsActive);
        Assert.NotNull(commodity.UpdatedAt);

        commodity.Activate();

        Assert.True(commodity.IsActive);
    }
}
