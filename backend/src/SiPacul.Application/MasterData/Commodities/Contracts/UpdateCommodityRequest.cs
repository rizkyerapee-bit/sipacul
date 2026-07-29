namespace SiPacul.Application.MasterData.Commodities.Contracts;

public sealed record UpdateCommodityRequest(
    string Name,
    Guid CommodityCategoryId,
    string? ScientificName,
    string? Description);
