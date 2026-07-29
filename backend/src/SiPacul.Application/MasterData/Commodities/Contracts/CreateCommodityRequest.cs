namespace SiPacul.Application.MasterData.Commodities.Contracts;

public sealed record CreateCommodityRequest(
    string Code,
    string Name,
    Guid CommodityCategoryId,
    string? ScientificName,
    string? Description);
