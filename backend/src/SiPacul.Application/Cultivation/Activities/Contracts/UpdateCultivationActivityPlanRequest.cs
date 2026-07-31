using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record UpdateCultivationActivityPlanRequest(
    string Name,
    CultivationActivityType ActivityType,
    DateOnly PlannedDate,
    string? Notes);
