namespace SiPacul.Application.MasterData.CommodityCategories.Contracts;

public sealed record UpdateCommodityCategoryRequest(
    string Name,
    string? Description);
