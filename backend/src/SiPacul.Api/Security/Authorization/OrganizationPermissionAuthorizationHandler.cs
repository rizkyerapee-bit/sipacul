using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SiPacul.Application.Security.Authorization.Services;

namespace SiPacul.Api.Security.Authorization;

public sealed class
    OrganizationPermissionAuthorizationHandler :
        AuthorizationHandler<
            OrganizationPermissionRequirement>
{
    private readonly IOrganizationPermissionService
        _permissionService;

    public OrganizationPermissionAuthorizationHandler(
        IOrganizationPermissionService permissionService)
    {
        ArgumentNullException.ThrowIfNull(
            permissionService);

        _permissionService = permissionService;
    }

    protected override async Task
        HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OrganizationPermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated !=
            true)
        {
            return;
        }

        var userIdValue =
            context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            return;
        }

        if (context.Resource is not
            HttpContext httpContext)
        {
            return;
        }

        if (!TryGetOrganizationId(
                httpContext,
                out var organizationId))
        {
            return;
        }

        var granted =
            await _permissionService
                .HasPermissionAsync(
                    userId,
                    organizationId,
                    requirement.Permission,
                    httpContext.RequestAborted);

        if (granted)
        {
            context.Succeed(requirement);
        }
    }

    private static bool TryGetOrganizationId(
        HttpContext httpContext,
        out Guid organizationId)
    {
        organizationId = Guid.Empty;

        var routeValue =
            httpContext.Request.RouteValues[
                OrganizationPermissionPolicies
                    .OrganizationRouteValueName];

        if (routeValue is Guid routeGuid)
        {
            organizationId = routeGuid;

            return routeGuid != Guid.Empty;
        }

        var routeText =
            Convert.ToString(
                routeValue,
                CultureInfo.InvariantCulture);

        return Guid.TryParse(
                routeText,
                out organizationId) &&
            organizationId != Guid.Empty;
    }
}
