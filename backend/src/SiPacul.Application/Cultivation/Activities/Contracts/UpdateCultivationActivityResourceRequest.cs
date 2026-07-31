namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record UpdateCultivationActivityResourceRequest(
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    string? Notes);
