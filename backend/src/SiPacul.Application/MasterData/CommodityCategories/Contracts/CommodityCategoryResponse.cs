namespace SiPacul.Application.MasterData.CommodityCategories.Contracts;

public sealed record CommodityCategoryResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
