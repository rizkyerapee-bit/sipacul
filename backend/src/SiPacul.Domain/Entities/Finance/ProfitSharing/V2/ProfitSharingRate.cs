namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public readonly record struct ProfitSharingRate
{
    private ProfitSharingRate(
        decimal numerator,
        decimal denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public decimal Numerator { get; }

    public decimal Denominator { get; }

    public decimal Value =>
        Numerator / Denominator;

    public static ProfitSharingRate FromFraction(
        decimal numerator,
        decimal denominator)
    {
        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(denominator),
                "Rate denominator must be greater than zero.");
        }

        if (numerator < 0 || numerator > denominator)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numerator),
                "Rate must be between zero and one.");
        }

        return new ProfitSharingRate(
            numerator,
            denominator);
    }

    public static ProfitSharingRate FromPercentage(
        decimal percentage)
    {
        return FromFraction(
            percentage,
            100m);
    }

    internal decimal ApplyTo(decimal amount)
    {
        return Math.Round(
            amount * Numerator / Denominator,
            2,
            MidpointRounding.AwayFromZero);
    }
}
