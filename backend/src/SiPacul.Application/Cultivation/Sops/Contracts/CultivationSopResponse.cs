namespace SiPacul.Application.Cultivation.Sops.Contracts;

public sealed record CultivationSopResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CommodityId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<CultivationSopStepResponse> Steps);
