using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Security.Authorization.Services;

namespace SiPacul.Api.Tests.Security.Authorization;

public sealed class
    OrganizationPermissionAuthorizationHandlerTests
{
    private static readonly Guid UserId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid OrganizationId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task HandleAsync_WhenPermissionGranted_ShouldSucceed()
    {
        var service =
            new StubOrganizationPermissionService
            {
                Granted = true
            };

        var context =
            CreateAuthorizationContext(
                CreateAuthenticatedUser(),
                OrganizationId.ToString());

        var handler =
            new OrganizationPermissionAuthorizationHandler(
                service);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);

        Assert.Equal(1, service.CallCount);
        Assert.Equal(UserId, service.LastUserId);
        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(
            Permissions.LandsRead,
            service.LastPermission);
    }

    [Fact]
    public async Task HandleAsync_WhenPermissionDenied_ShouldNotSucceed()
    {
        var service =
            new StubOrganizationPermissionService
            {
                Granted = false
            };

        var context =
            CreateAuthorizationContext(
                CreateAuthenticatedUser(),
                OrganizationId.ToString());

        var handler =
            new OrganizationPermissionAuthorizationHandler(
                service);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithoutUserIdentifier_ShouldNotQuery()
    {
        var service =
            new StubOrganizationPermissionService
            {
                Granted = true
            };

        var identity =
            new ClaimsIdentity(
                authenticationType: "Test");

        var context =
            CreateAuthorizationContext(
                new ClaimsPrincipal(identity),
                OrganizationId.ToString());

        var handler =
            new OrganizationPermissionAuthorizationHandler(
                service);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithoutOrganizationRoute_ShouldNotQuery()
    {
        var service =
            new StubOrganizationPermissionService
            {
                Granted = true
            };

        var context =
            CreateAuthorizationContext(
                CreateAuthenticatedUser(),
                organizationRouteValue: null);

        var handler =
            new OrganizationPermissionAuthorizationHandler(
                service);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidOrganizationRoute_ShouldNotQuery()
    {
        var service =
            new StubOrganizationPermissionService
            {
                Granted = true
            };

        var context =
            CreateAuthorizationContext(
                CreateAuthenticatedUser(),
                "not-a-guid");

        var handler =
            new OrganizationPermissionAuthorizationHandler(
                service);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Equal(0, service.CallCount);
    }

    private static AuthorizationHandlerContext
        CreateAuthorizationContext(
            ClaimsPrincipal principal,
            string? organizationRouteValue)
    {
        var requirement =
            new OrganizationPermissionRequirement(
                Permissions.LandsRead);

        var httpContext =
            new DefaultHttpContext();

        if (organizationRouteValue is not null)
        {
            httpContext.Request.RouteValues[
                OrganizationPermissionPolicies
                    .OrganizationRouteValueName] =
                        organizationRouteValue;
        }

        return new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            httpContext);
    }

    private static ClaimsPrincipal
        CreateAuthenticatedUser()
    {
        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        UserId.ToString())
                },
                authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    private sealed class
        StubOrganizationPermissionService :
            IOrganizationPermissionService
    {
        public bool Granted { get; set; }

        public int CallCount { get; private set; }

        public Guid LastUserId { get; private set; }

        public Guid LastOrganizationId
        {
            get;
            private set;
        }

        public string? LastPermission
        {
            get;
            private set;
        }

        public Task<bool> HasPermissionAsync(
            Guid userId,
            Guid organizationId,
            string? permission,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            CallCount++;
            LastUserId = userId;
            LastOrganizationId = organizationId;
            LastPermission = permission;

            return Task.FromResult(Granted);
        }
    }
}
