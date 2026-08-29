using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SiPacul.Api.Common.Http;

namespace SiPacul.Api.Tests.Common.Http;

public sealed class PublicResponseSecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task LiveEndpoint_ShouldReturnCompleteSecurityHeaderBaseline()
    {
        await using var factory =
            new PublicResponseSecurityHeadersApiFactory();
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(
            "/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeader(
            response,
            "Content-Security-Policy",
            PublicResponseSecurityHeadersMiddleware.ContentSecurityPolicy);
        AssertHeader(
            response,
            "Referrer-Policy",
            PublicResponseSecurityHeadersMiddleware.ReferrerPolicy);
        AssertHeader(
            response,
            "X-Content-Type-Options",
            PublicResponseSecurityHeadersMiddleware.XContentTypeOptions);
        AssertHeader(
            response,
            "X-Frame-Options",
            PublicResponseSecurityHeadersMiddleware.XFrameOptions);
        Assert.False(
            response.Headers.Contains(
                "Strict-Transport-Security"));
    }

    private static void AssertHeader(
        HttpResponseMessage response,
        string name,
        string expectedValue)
    {
        Assert.True(
            response.Headers.TryGetValues(
                name,
                out var values));
        Assert.Equal(
            expectedValue,
            Assert.Single(values));
    }

    private sealed class PublicResponseSecurityHeadersApiFactory :
        WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;" +
                "Port=1;" +
                "Database=sipacul_security_header_tests;" +
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
