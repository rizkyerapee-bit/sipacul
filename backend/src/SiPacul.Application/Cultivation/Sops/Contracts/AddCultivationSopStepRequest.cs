namespace SiPacul.Application.Cultivation.Sops.Contracts;

public sealed record AddCultivationSopStepRequest(
    string Name,
    string? Description,
    int PlannedDayOffset,
    int EstimatedDurationDays,
    bool IsRequired);
