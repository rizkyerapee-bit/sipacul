using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiPacul.Application.Security.Authorization.Services;

namespace SiPacul.Api.Tests.Security.Authorization;

internal static class OrganizationAuthorizationTestSupport
{
    public const string UnauthenticatedHeaderName =
        "X-SiPacul-Test-Unauthenticated";

    private const string SchemeName =
        "SiPacul.Api.Tests.Authentication";

    public static IServiceCollection
        AddOrganizationAuthorizationForTests(
            this IServiceCollection services,
            ConfigurableOrganizationPermissionService
                permissionService)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(permissionService);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    SchemeName;

                options.DefaultChallengeScheme =
                    SchemeName;

                options.DefaultForbidScheme =
                    SchemeName;
            })
            .AddScheme<
                AuthenticationSchemeOptions,
                TestAuthenticationHandler>(
                    SchemeName,
                    _ => { });

        services.RemoveAll<
            IOrganizationPermissionService>();

        services.AddSingleton<
            IOrganizationPermissionService>(
                permissionService);

        return services;
    }

    private sealed class TestAuthenticationHandler :
        AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private static readonly Guid UserId =
            Guid.Parse(
                "10000000-0000-0000-0000-000000000001");

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions>
                options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult>
            HandleAuthenticateAsync()
        {
            if (Request.Headers.ContainsKey(
                    UnauthenticatedHeaderName))
            {
                return Task.FromResult(
                    AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        UserId.ToString())
                },
                Scheme.Name);

            var principal =
                new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(
                principal,
                Scheme.Name);

            return Task.FromResult(
                AuthenticateResult.Success(ticket));
        }
    }
}

internal sealed class
    ConfigurableOrganizationPermissionService :
        IOrganizationPermissionService
{
    public bool Granted { get; set; } = true;

    public int CallCount { get; private set; }

    public Guid LastUserId { get; private set; }

    public Guid LastOrganizationId { get; private set; }

    public string? LastPermission { get; private set; }

    public Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string? permission,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CallCount++;
        LastUserId = userId;
        LastOrganizationId = organizationId;
        LastPermission = permission;

        return Task.FromResult(Granted);
    }
}
