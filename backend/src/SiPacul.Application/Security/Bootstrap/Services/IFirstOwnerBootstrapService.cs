using SiPacul.Application.Security.Bootstrap.Contracts;

namespace SiPacul.Application.Security.Bootstrap.Services;

public interface IFirstOwnerBootstrapService
{
    Task<FirstOwnerBootstrapStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default);

    Task<FirstOwnerBootstrapResult> BootstrapAsync(
        FirstOwnerBootstrapRequest request,
        string? suppliedToken,
        CancellationToken cancellationToken = default);
}
