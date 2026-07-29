namespace SiPacul.Application.Cultivation.Sops.Contracts;

public sealed record CreateCultivationSopRequest(
    Guid CommodityId,
    string Name,
    string? Description);
