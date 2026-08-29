using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Common.Http;
using SiPacul.Application.Security.Bootstrap;
using SiPacul.Application.Security.Bootstrap.Contracts;
using SiPacul.Application.Security.Bootstrap.Services;

namespace SiPacul.Api.Tests.Common.Http;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task UnexpectedException_ShouldReturnSanitizedProblem()
    {
        const string confidentialMessage =
            "Database password and stack detail must stay private.";

        await using var factory =
            new GlobalFailureApiFactory(
                new InvalidOperationException(
                    confidentialMessage));
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(
            "/api/v1/bootstrap/status");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString() ??
                string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "no-cache",
            string.Join(
                ",",
                response.Headers.GetValues("Pragma")),
            StringComparison.OrdinalIgnoreCase);

        var content =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(content);
        var problem = document.RootElement;

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            problem.GetProperty("status").GetInt32());
        Assert.Equal(
            "Unexpected server error",
            problem.GetProperty("title").GetString());
        Assert.Equal(
            "An unexpected server error occurred.",
            problem.GetProperty("detail").GetString());
        Assert.Equal(
            GlobalExceptionHandler.ErrorCode,
            problem.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.GetProperty("traceId")
                    .GetString()));
        Assert.DoesNotContain(
            confidentialMessage,
            content,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            nameof(InvalidOperationException),
            content,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        StatusCodes.Status400BadRequest,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        StatusCodes.Status422UnprocessableEntity,
        StatusCodes.Status422UnprocessableEntity)]
    [InlineData(
        StatusCodes.Status500InternalServerError,
        StatusCodes.Status400BadRequest)]
    public async Task BadHttpRequestException_ShouldRemainClientError(
        int exceptionStatusCode,
        int expectedStatusCode)
    {
        const string confidentialMessage =
            "Malformed parameter value must stay private.";

        await using var factory =
            new GlobalFailureApiFactory(
                new BadHttpRequestException(
                    confidentialMessage,
                    exceptionStatusCode));
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(
            "/api/v1/bootstrap/status");

        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString() ??
                string.Empty,
            StringComparison.OrdinalIgnoreCase);

        var content =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(content);
        var problem = document.RootElement;

        Assert.Equal(
            expectedStatusCode,
            problem.GetProperty("status").GetInt32());
        Assert.Equal(
            "Invalid request",
            problem.GetProperty("title").GetString());
        Assert.Equal(
            "The request could not be processed.",
            problem.GetProperty("detail").GetString());
        Assert.Equal(
            GlobalExceptionHandler.InvalidRequestErrorCode,
            problem.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.GetProperty("traceId")
                    .GetString()));
        Assert.DoesNotContain(
            confidentialMessage,
            content,
            StringComparison.Ordinal);
    }

    private sealed class GlobalFailureApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly Exception _exception;

        public GlobalFailureApiFactory(
            Exception exception)
        {
            _exception = exception;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;" +
                "Port=1;" +
                "Database=sipacul_failure_tests;" +
                "Username=sipacul_test;" +
                "Password=sipacul_test;" +
                "Timeout=1;" +
                "Command Timeout=1;" +
                "Pooling=false");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IFirstOwnerBootstrapService>();
                services.AddSingleton<
                    IFirstOwnerBootstrapService>(
                    new ThrowingBootstrapService(
                        _exception));
            });
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

    private sealed class ThrowingBootstrapService :
        IFirstOwnerBootstrapService
    {
        private readonly Exception _exception;

        public ThrowingBootstrapService(
            Exception exception)
        {
            _exception = exception;
        }

        public Task<FirstOwnerBootstrapStatusResponse>
            GetStatusAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromException<
                FirstOwnerBootstrapStatusResponse>(
                _exception);
        }

        public Task<FirstOwnerBootstrapResult>
            BootstrapAsync(
                FirstOwnerBootstrapRequest request,
                string? suppliedToken,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromException<
                FirstOwnerBootstrapResult>(
                _exception);
        }
    }
}
