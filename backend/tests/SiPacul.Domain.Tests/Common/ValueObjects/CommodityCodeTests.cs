using SiPacul.Domain.Common.ValueObjects;

namespace SiPacul.Domain.Tests.Common.ValueObjects;

public sealed class CommodityCodeTests
{
    [Fact]
    public void Create_WithValidValue_ShouldCreateCommodityCode()
    {
        var code = CommodityCode.Create("PADI");

        Assert.Equal("PADI", code.Value);
    }

    [Fact]
    public void Create_WithLowercaseAndWhitespace_ShouldNormalizeValue()
    {
        var code = CommodityCode.Create("  padi_merah  ");

        Assert.Equal("PADI_MERAH", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_ShouldThrowArgumentException(string value)
    {
        var action = () => CommodityCode.Create(value);

        var exception = Assert.Throws<ArgumentException>(action);

        Assert.StartsWith(
            "Commodity code cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void Create_WithValueExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var value = new string('A', CommodityCode.MaxLength + 1);

        var action = () => CommodityCode.Create(value);

        var exception = Assert.Throws<ArgumentException>(action);

        Assert.StartsWith(
            $"Commodity code cannot exceed {CommodityCode.MaxLength} characters.",
            exception.Message);
    }

    [Theory]
    [InlineData("PADI MERAH")]
    [InlineData("PADI@MERAH")]
    [InlineData("PADI.MERAH")]
    public void Create_WithInvalidCharacters_ShouldThrowArgumentException(
        string value)
    {
        var action = () => CommodityCode.Create(value);

        var exception = Assert.Throws<ArgumentException>(action);

        Assert.StartsWith(
            "Commodity code may only contain letters, numbers, hyphens, and underscores.",
            exception.Message);
    }

    [Theory]
    [InlineData("PADI-01")]
    [InlineData("PADI_01")]
    [InlineData("123")]
    public void Create_WithSupportedCharacters_ShouldCreateCommodityCode(
        string value)
    {
        var code = CommodityCode.Create(value);

        Assert.Equal(value, code.Value);
    }
}
