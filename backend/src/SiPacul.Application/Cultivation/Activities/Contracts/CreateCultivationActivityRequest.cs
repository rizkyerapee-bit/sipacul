using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record CreateCultivationActivityRequest(
    string Code,
    string Name,
    CultivationActivityType ActivityType,
    DateOnly PlannedDate,
    Guid? CultivationSopId,
    Guid? CultivationSopStepId,
    string? Notes);
