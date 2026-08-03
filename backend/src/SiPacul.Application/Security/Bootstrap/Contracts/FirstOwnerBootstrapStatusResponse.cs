namespace SiPacul.Application.Security.Bootstrap.Contracts;

public sealed record FirstOwnerBootstrapStatusResponse(
    bool IsConfigured,
    bool IsInitialized,
    bool CanBootstrap);
