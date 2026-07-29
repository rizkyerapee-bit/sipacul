namespace SiPacul.Application.MasterData.Commodities.Contracts;

public sealed record CommodityResponse(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    Guid CommodityCategoryId,
    string? ScientificName,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
