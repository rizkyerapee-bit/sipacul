namespace SiPacul.Application.Cultivation.Sops.Contracts;

public sealed record UpdateCultivationSopStepRequest(
    string Name,
    string? Description,
    int PlannedDayOffset,
    int EstimatedDurationDays,
    bool IsRequired);
