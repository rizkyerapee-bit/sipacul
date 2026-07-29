namespace SiPacul.Application.Cultivation.Sops.Contracts;

public sealed record CultivationSopStepResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CultivationSopId,
    int Sequence,
    string Name,
    string? Description,
    int PlannedDayOffset,
    int EstimatedDurationDays,
    bool IsRequired,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
