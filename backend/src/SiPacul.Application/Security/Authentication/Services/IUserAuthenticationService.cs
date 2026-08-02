using System.Security.Claims;
using SiPacul.Application.Security.Authentication.Contracts;

namespace SiPacul.Application.Security.Authentication.Services;

public interface IUserAuthenticationService
{
    Task<UserAuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(
        CancellationToken cancellationToken = default);

    Task<CurrentUserResponse?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
