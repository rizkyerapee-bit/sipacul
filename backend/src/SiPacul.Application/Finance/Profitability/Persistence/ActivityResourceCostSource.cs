using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ActivityResourceCostSource(
    CultivationActivityStatus ActivityStatus,
    DateOnly? ActualStartDate,
    decimal TotalCost);
