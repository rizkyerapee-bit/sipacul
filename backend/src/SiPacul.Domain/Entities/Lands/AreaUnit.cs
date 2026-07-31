namespace SiPacul.Domain.Entities.Lands;

public enum AreaUnit
{
    SquareMeter = 1,

    Hectare = 2
}

internal static class AreaUnitConverter
{
    private const decimal SquareMetersPerHectare = 10_000m;

    public static decimal ToSquareMeters(
        decimal area,
        AreaUnit unit)
    {
        return unit switch
        {
            AreaUnit.SquareMeter => area,
            AreaUnit.Hectare =>
                area * SquareMetersPerHectare,
            _ => throw new ArgumentOutOfRangeException(
                nameof(unit),
                unit,
                "Area unit is not supported.")
        };
    }
}
