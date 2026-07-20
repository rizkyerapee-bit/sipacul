using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Events.MasterData;

namespace SiPacul.Domain.Tests.Entities.MasterData;

public sealed class CommodityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCommodity()
    {
        // Arrange
        var code = new CommodityCode("PADI");
        var categoryId = Guid.NewGuid();

        // Act
        var commodity = Commodity.Create(
            code,
            "Padi",
            categoryId,
            "Oryza sativa",
            "Tanaman pangan utama.");

        // Assert
        Assert.NotEqual(Guid.Empty, commodity.Id);
        Assert.Equal(code, commodity.Code);
        Assert.Equal("Padi", commodity.Name);
        Assert.Equal(categoryId, commodity.CommodityCategoryId);
        Assert.Equal("Oryza sativa", commodity.ScientificName);
        Assert.Equal("Tanaman pangan utama.", commodity.Description);
    }

    [Fact]
    public void Create_WithWhitespaceAroundText_ShouldTrimText()
    {
        // Arrange
        var code = new CommodityCode("CABAI");
        var categoryId = Guid.NewGuid();

        // Act
        var commodity = Commodity.Create(
            code,
            "  Cabai Merah  ",
            categoryId,
            "  Capsicum annuum  ",
            "  Komoditas hortikultura.  ");

        // Assert
        Assert.Equal("Cabai Merah", commodity.Name);
        Assert.Equal("Capsicum annuum", commodity.ScientificName);
        Assert.Equal("Komoditas hortikultura.", commodity.Description);
    }

    [Fact]
    public void Create_WithValidData_ShouldRaiseCommodityCreatedDomainEvent()
    {
        // Arrange
        var code = new CommodityCode("JAGUNG");
        var categoryId = Guid.NewGuid();

        // Act
        var commodity = Commodity.Create(
            code,
            "Jagung",
            categoryId,
            "Zea mays",
            null);

        // Assert
        Assert.Single(commodity.DomainEvents);
        Assert.IsType<CommodityCreatedDomainEvent>(
            commodity.DomainEvents.Single());
    }

    [Fact]
    public void Create_ShouldRaiseDomainEventWithCorrectData()
    {
        // Arrange
        var code = new CommodityCode("KEDELAI");
        var categoryId = Guid.NewGuid();

        // Act
        var commodity = Commodity.Create(
            code,
            "Kedelai",
            categoryId,
            "Glycine max",
            null);

        // Assert
        var domainEvent = Assert.IsType<CommodityCreatedDomainEvent>(
            commodity.DomainEvents.Single());

        Assert.Equal(commodity.Id, domainEvent.CommodityId);
        Assert.Equal(commodity.Name, domainEvent.CommodityName);
        Assert.Equal(categoryId, domainEvent.CommodityCategoryId);
    }

    [Fact]
    public void ClearDomainEvents_AfterCommodityCreated_ShouldRemoveAllEvents()
    {
        // Arrange
        var commodity = Commodity.Create(
            new CommodityCode("TOMAT"),
            "Tomat",
            Guid.NewGuid(),
            "Solanum lycopersicum",
            null);

        Assert.Single(commodity.DomainEvents);

        // Act
        commodity.ClearDomainEvents();

        // Assert
        Assert.Empty(commodity.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var code = new CommodityCode("INVALID");
        var categoryId = Guid.NewGuid();

        // Act
        var action = () => Commodity.Create(
            code,
            name,
            categoryId,
            null,
            null);

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            "Commodity name cannot be empty.",
            exception.Message);
    }
}
