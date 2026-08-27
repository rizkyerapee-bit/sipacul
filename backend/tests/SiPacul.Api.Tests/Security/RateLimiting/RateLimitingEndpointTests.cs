using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SiPacul.Api.Security.RateLimiting;
using SiPacul.Application.Security.Authentication.Contracts;

namespace SiPacul.Api.Tests.Security.RateLimiting;

public sealed class RateLimitingEndpointTests
{
    private const string LoginPath =
        "/api/v1/auth/login";

    [Fact]
    public void GlobalLimiter_ShouldLimitApiAndExemptHealth()
    {
        using var factory =
            new RateLimitingApiFactory();

        var options =
            factory.Services
                .GetRequiredService<
                    IOptions<RateLimiterOptions>>()
                .Value;

        Assert.Equal(
            StatusCodes.Status429TooManyRequests,
            options.RejectionStatusCode);

        Assert.NotNull(options.GlobalLimiter);

        var apiContext = CreateContext(
            "/api/v1/organizations",
            "192.0.2.10");

        for (var index = 0;
             index < SiPaculRateLimitingDefaults
                 .GlobalPermitLimit;
             index++)
        {
            using var lease =
                options.GlobalLimiter!
                    .AttemptAcquire(apiContext);

            Assert.True(lease.IsAcquired);
        }

        using (var rejectedLease =
               options.GlobalLimiter!
                   .AttemptAcquire(apiContext))
        {
            Assert.False(rejectedLease.IsAcquired);
        }

        var healthContext = CreateContext(
            "/health/ready",
            "192.0.2.10");

        for (var index = 0;
             index < SiPaculRateLimitingDefaults
                 .GlobalPermitLimit + 1;
             index++)
        {
            using var lease =
                options.GlobalLimiter!
                    .AttemptAcquire(healthContext);

            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public void SensitiveEndpoints_ShouldCarryStrictPolicies()
    {
        using var factory =
            new RateLimitingApiFactory();

        var dataSource =
            factory.Services
                .GetRequiredService<EndpointDataSource>();

        AssertPolicy(
            dataSource,
            LoginPath,
            SiPaculRateLimitingDefaults
                .AuthenticationPolicyName);

        AssertPolicy(
            dataSource,
            "/api/v1/bootstrap/owner",
            SiPaculRateLimitingDefaults
                .BootstrapPolicyName);
    }

    [Fact]
    public async Task LoginBurst_ShouldReturnStructured429()
    {
        using var factory =
            new RateLimitingApiFactory();

        using var client =
            factory.CreateHttpsClient();

        for (var index = 0;
             index < SiPaculRateLimitingDefaults
                 .AuthenticationPermitLimit;
             index++)
        {
            using var response =
                await SendLoginWithoutAntiforgeryAsync(
                    client);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        using var rejected =
            await SendLoginWithoutAntiforgeryAsync(
                client);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejected.StatusCode);

        Assert.Equal(
            "application/problem+json",
            rejected.Content.Headers.ContentType?.MediaType);

        var content =
            await rejected.Content.ReadAsStringAsync();

        Assert.Contains(
            SiPaculRateLimitingDefaults
                .RejectionCode,
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "no-store",
            rejected.Headers.CacheControl?.ToString() ??
                string.Empty,
            StringComparison.OrdinalIgnoreCase);

        using var health =
            await client.GetAsync(
                "/health/live");

        Assert.Equal(
            HttpStatusCode.OK,
            health.StatusCode);
    }

    private static DefaultHttpContext CreateContext(
        string path,
        string address)
    {
        var context =
            new DefaultHttpContext();

        context.Request.Path = path;

        context.Connection.RemoteIpAddress =
            IPAddress.Parse(address);

        return context;
    }

    private static void AssertPolicy(
        EndpointDataSource dataSource,
        string route,
        string expectedPolicy)
    {
        var endpoint =
            Assert.Single(
                dataSource.Endpoints
                    .OfType<RouteEndpoint>(),
                candidate =>
                    string.Equals(
                        candidate.RoutePattern.RawText,
                        route,
                        StringComparison.Ordinal));

        var metadata =
            endpoint.Metadata.GetMetadata<
                EnableRateLimitingAttribute>();

        Assert.NotNull(metadata);

        Assert.Equal(
            expectedPolicy,
            metadata!.PolicyName);
    }

    private static Task<HttpResponseMessage>
        SendLoginWithoutAntiforgeryAsync(
            HttpClient client)
    {
        return client.PostAsJsonAsync(
            LoginPath,
            new LoginRequest(
                "owner@example.com",
                "ValidPassword!123",
                false));
    }

    private sealed class RateLimitingApiFactory :
        WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;" +
                "Port=1;" +
                "Database=sipacul_rate_limit_tests;" +
                "Username=sipacul_test;" +
                "Password=sipacul_test;" +
                "Timeout=1;" +
                "Command Timeout=1;" +
                "Pooling=false");
        }

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("https://localhost"),
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }
    }
}
