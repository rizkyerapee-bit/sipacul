using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SiPacul.Api.Tests.Health;

public sealed class OperationalHealthEndpointTests
{
    [Fact]
    public async Task Live_ShouldStayHealthyWithoutDatabaseConnection()
    {
        await using var factory =
            new OperationalHealthApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content
            .ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Empty(body.Checks);
        AssertNoStore(response);
    }

    [Fact]
    public async Task Ready_ShouldReturnServiceUnavailableWhenDatabaseIsUnavailable()
    {
        await using var factory =
            new OperationalHealthApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/health/ready");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        var body = await response.Content
            .ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Unhealthy", body.Status);
        Assert.Equal("Unhealthy", body.Checks["database"]);
        AssertNoStore(response);
    }

    private static void AssertNoStore(
        HttpResponseMessage response)
    {
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString() ??
                string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed record HealthResponse(
        string Status,
        Dictionary<string, string> Checks,
        double DurationMilliseconds);

    private sealed class OperationalHealthApiFactory :
        WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;" +
                "Port=1;" +
                "Database=sipacul_health_tests;" +
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
