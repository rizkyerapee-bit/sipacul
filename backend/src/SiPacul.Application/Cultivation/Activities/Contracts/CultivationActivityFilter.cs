using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record CultivationActivityFilter(
    CultivationActivityStatus? Status = null,
    CultivationActivityType? ActivityType = null,
    DateOnly? PlannedFrom = null,
    DateOnly? PlannedTo = null,
    Guid? CultivationSopStepId = null);
