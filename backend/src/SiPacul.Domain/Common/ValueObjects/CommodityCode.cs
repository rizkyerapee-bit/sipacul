using System.Text.RegularExpressions;

namespace SiPacul.Domain.Common.ValueObjects;

public sealed partial record CommodityCode
{
    public const int MaxLength = 20;

    private CommodityCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static CommodityCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Commodity code cannot be empty.",
                nameof(value));
        }

        var normalizedValue = value.Trim().ToUpperInvariant();

        if (normalizedValue.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Commodity code cannot exceed {MaxLength} characters.",
                nameof(value));
        }

        if (!CommodityCodePattern().IsMatch(normalizedValue))
        {
            throw new ArgumentException(
                "Commodity code may only contain letters, numbers, hyphens, and underscores.",
                nameof(value));
        }

        return new CommodityCode(normalizedValue);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9_-]+$")]
    private static partial Regex CommodityCodePattern();
}
