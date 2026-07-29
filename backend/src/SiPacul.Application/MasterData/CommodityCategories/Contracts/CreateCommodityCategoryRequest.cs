namespace SiPacul.Application.MasterData.CommodityCategories.Contracts;

public sealed record CreateCommodityCategoryRequest(
    string Name,
    string? Description);
