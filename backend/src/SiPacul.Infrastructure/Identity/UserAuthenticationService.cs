using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiPacul.Application.Security.Authentication.Contracts;
using SiPacul.Application.Security.Authentication.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;

namespace SiPacul.Infrastructure.Identity;

public sealed class UserAuthenticationService :
    IUserAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly SiPaculDbContext _dbContext;
    private readonly ILogger<UserAuthenticationService> _logger;

    public UserAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        SiPaculDbContext dbContext,
        ILogger<UserAuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserAuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return UserAuthenticationResult.Failed();
        }

        var user =
            await _userManager.FindByEmailAsync(email);

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation(
                "Authentication failed for supplied credentials.");

            return UserAuthenticationResult.Failed();
        }

        var signInResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                password,
                lockoutOnFailure: true);

        cancellationToken.ThrowIfCancellationRequested();

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning(
                    "Authentication rejected because user {UserId} " +
                    "is locked out.",
                    user.Id);
            }
            else
            {
                _logger.LogInformation(
                    "Authentication failed for user {UserId}.",
                    user.Id);
            }

            return UserAuthenticationResult.Failed();
        }

        user.RecordSuccessfulLogin();

        var updateResult =
            await _userManager.UpdateAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        if (!updateResult.Succeeded)
        {
            _logger.LogError(
                "Unable to persist successful login for user " +
                "{UserId}. Identity errors: {Errors}",
                user.Id,
                string.Join(
                    ", ",
                    updateResult.Errors.Select(error =>
                        error.Code)));

            return UserAuthenticationResult.Failed();
        }

        await _signInManager.SignInAsync(
            user,
            request.RememberMe);

        var response =
            await CreateCurrentUserResponseAsync(
                user,
                cancellationToken);

        _logger.LogInformation(
            "User {UserId} authenticated successfully.",
            user.Id);

        return UserAuthenticationResult.Success(response);
    }

    public async Task SignOutAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _signInManager.SignOutAsync();
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        cancellationToken.ThrowIfCancellationRequested();

        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var user =
            await _userManager.GetUserAsync(principal);

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null || !user.IsActive)
        {
            await _signInManager.SignOutAsync();

            return null;
        }

        return await CreateCurrentUserResponseAsync(
            user,
            cancellationToken);
    }

    private async Task<CurrentUserResponse>
        CreateCurrentUserResponseAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
    {
        var membershipRows =
            await _dbContext.OrganizationMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.UserId == user.Id &&
                    membership.Status ==
                        OrganizationMembershipStatus.Active &&
                    !membership.IsDeleted)
                .OrderBy(membership =>
                    membership.OrganizationId)
                .ThenBy(membership => membership.Id)
                .Select(membership =>
                    new
                    {
                        membership.Id,
                        membership.OrganizationId,
                        membership.Role
                    })
                .ToArrayAsync(cancellationToken);

        var memberships =
            membershipRows
                .Select(membership =>
                    new CurrentUserMembershipResponse(
                        membership.Id,
                        membership.OrganizationId,
                        membership.Role,
                        RolePermissionCatalog
                            .GetPermissions(
                                membership.Role)
                            .ToArray()))
                .ToArray();

        var email =
            user.Email ??
            user.UserName ??
            string.Empty;

        return new CurrentUserResponse(
            user.Id,
            email,
            user.EmailConfirmed,
            user.LastLoginAt,
            memberships);
    }
}
