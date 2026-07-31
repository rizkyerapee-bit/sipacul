using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record AddCultivationActivityResourceRequest(
    CultivationResourceType ResourceType,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    string? Notes);
