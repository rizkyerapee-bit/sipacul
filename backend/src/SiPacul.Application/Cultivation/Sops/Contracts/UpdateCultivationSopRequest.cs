namespace SiPacul.Application.Cultivation.Sops.Contracts;

public sealed record UpdateCultivationSopRequest(
    string Name,
    string? Description);
