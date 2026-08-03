using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Security;
using SiPacul.Application.Security.Authentication;
using SiPacul.Application.Security.Authentication.Contracts;
using SiPacul.Application.Security.Bootstrap;
using SiPacul.Application.Security.Bootstrap.Contracts;
using SiPacul.Application.Security.Bootstrap.Services;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Api.Tests.Security.Bootstrap;

public sealed class BootstrapEndpointTests
{
    private const string CsrfPath =
        "/api/v1/auth/csrf";

    private const string StatusPath =
        "/api/v1/bootstrap/status";

    private const string OwnerPath =
        "/api/v1/bootstrap/owner";

    private const string ValidBootstrapToken =
        "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Status_ShouldReturnStateAndNoStoreHeaders()
    {
        var service =
            new StubBootstrapService
            {
                Status =
                    new FirstOwnerBootstrapStatusResponse(
                        true,
                        false,
                        true)
            };

        using var factory =
            new BootstrapApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(StatusPath);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                FirstOwnerBootstrapStatusResponse>();

        Assert.NotNull(body);
        Assert.True(body!.IsConfigured);
        Assert.False(body.IsInitialized);
        Assert.True(body.CanBootstrap);
        Assert.Equal(1, service.StatusCallCount);

        Assert.True(
            response.Headers.TryGetValues(
                "Cache-Control",
                out var cacheControl));

        Assert.Contains(
            cacheControl,
            value =>
                value.Contains(
                    "no-store",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Owner_WithValidHeaders_ShouldReturnCreated()
    {
        var expected =
            CreateBootstrapResponse();

        var service =
            new StubBootstrapService
            {
                Result =
                    FirstOwnerBootstrapResult.Success(
                        expected)
            };

        using var factory =
            new BootstrapApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var csrfToken =
            await GetAntiforgeryTokenAsync(client);

        var request =
            CreateRequest();

        var response =
            await SendOwnerRequestAsync(
                client,
                request,
                csrfToken,
                ValidBootstrapToken);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.Equal(
            StatusPath,
            response.Headers.Location?.OriginalString);

        var body =
            await response.Content.ReadFromJsonAsync<
                FirstOwnerBootstrapResponse>();

        Assert.NotNull(body);
        Assert.Equal(expected.UserId, body!.UserId);
        Assert.Equal(
            expected.OrganizationId,
            body.OrganizationId);
        Assert.Equal(
            expected.MembershipId,
            body.MembershipId);
        Assert.Equal(
            OrganizationRole.Owner,
            body.Role);

        Assert.Equal(1, service.BootstrapCallCount);
        Assert.Equal(
            ValidBootstrapToken,
            service.LastSuppliedToken);
        Assert.Equal(
            request,
            service.LastRequest);
    }

    [Fact]
    public async Task Owner_WithoutAntiforgery_ShouldReturnBadRequest()
    {
        var service =
            new StubBootstrapService();

        using var factory =
            new BootstrapApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                OwnerPath)
            {
                Content =
                    JsonContent.Create(
                        CreateRequest())
            };

        message.Headers.Add(
            SiPaculBootstrapDefaults.TokenHeaderName,
            ValidBootstrapToken);

        var response =
            await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            AuthenticationErrorCodes
                .InvalidAntiforgeryToken,
            content);

        Assert.Equal(
            0,
            service.BootstrapCallCount);
    }

    [Fact]
    public async Task Owner_WithoutBootstrapToken_ShouldReturnUnauthorized()
    {
        var service =
            new StubBootstrapService();

        using var factory =
            new BootstrapApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var csrfToken =
            await GetAntiforgeryTokenAsync(client);

        var response =
            await SendOwnerRequestAsync(
                client,
                CreateRequest(),
                csrfToken,
                null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .InvalidToken,
            content);

        Assert.Equal(
            0,
            service.BootstrapCallCount);
    }

    [Fact]
    public async Task Owner_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var service =
            CreateFailureService(
                FirstOwnerBootstrapFailure.InvalidToken);

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .InvalidToken,
            content);
    }

    [Fact]
    public async Task Owner_WhenNotConfigured_ShouldReturnUnavailable()
    {
        var service =
            CreateFailureService(
                FirstOwnerBootstrapFailure.NotConfigured);

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .NotConfigured,
            content);
    }

    [Fact]
    public async Task Owner_WhenInitialized_ShouldReturnConflict()
    {
        var service =
            CreateFailureService(
                FirstOwnerBootstrapFailure
                    .AlreadyInitialized);

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .AlreadyInitialized,
            content);
    }

    [Fact]
    public async Task Owner_WithInvalidRequest_ShouldReturnBadRequest()
    {
        var service =
            CreateFailureService(
                FirstOwnerBootstrapFailure.InvalidRequest);

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .InvalidRequest,
            content);
    }

    [Fact]
    public async Task Owner_WithIdentityFailure_ShouldReturnErrors()
    {
        var service =
            new StubBootstrapService
            {
                Result =
                    FirstOwnerBootstrapResult.Failed(
                        FirstOwnerBootstrapFailure
                            .IdentityValidationFailed,
                        "Owner account validation failed.",
                        new[]
                        {
                            "Passwords must have at least " +
                            "one non alphanumeric character."
                        })
            };

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .IdentityValidationFailed,
            content);

        Assert.Contains(
            "non alphanumeric",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Owner_WithDataConflict_ShouldReturnConflict()
    {
        var service =
            CreateFailureService(
                FirstOwnerBootstrapFailure.Conflict);

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes.Conflict,
            content);
    }

    [Fact]
    public async Task Owner_WithPersistenceFailure_ShouldReturnServerError()
    {
        var service =
            CreateFailureService(
                FirstOwnerBootstrapFailure
                    .PersistenceFailure);

        var response =
            await SendWithServiceAsync(service);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            FirstOwnerBootstrapErrorCodes
                .PersistenceFailure,
            content);
    }

    [Fact]
    public async Task UnknownBootstrapRoute_ShouldReturnNotFound()
    {
        using var factory =
            new BootstrapApiFactory(
                new StubBootstrapService());

        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(
                "/api/v1/bootstrap/unsupported");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static StubBootstrapService
        CreateFailureService(
            FirstOwnerBootstrapFailure failure)
    {
        return new StubBootstrapService
        {
            Result =
                FirstOwnerBootstrapResult.Failed(
                    failure,
                    "Expected bootstrap failure.")
        };
    }

    private static async Task<HttpResponseMessage>
        SendWithServiceAsync(
            StubBootstrapService service)
    {
        using var factory =
            new BootstrapApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var csrfToken =
            await GetAntiforgeryTokenAsync(client);

        return await SendOwnerRequestAsync(
            client,
            CreateRequest(),
            csrfToken,
            ValidBootstrapToken);
    }

    private static async Task<string>
        GetAntiforgeryTokenAsync(
            HttpClient client)
    {
        var response =
            await client.GetAsync(CsrfPath);

        response.EnsureSuccessStatusCode();

        var body =
            await response.Content.ReadFromJsonAsync<
                AntiforgeryTokenResponse>();

        Assert.NotNull(body);

        return body!.RequestToken;
    }

    private static Task<HttpResponseMessage>
        SendOwnerRequestAsync(
            HttpClient client,
            FirstOwnerBootstrapRequest request,
            string csrfToken,
            string? bootstrapToken)
    {
        var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                OwnerPath)
            {
                Content = JsonContent.Create(request)
            };

        message.Headers.Add(
            SiPaculAuthenticationDefaults
                .AntiforgeryHeaderName,
            csrfToken);

        if (bootstrapToken is not null)
        {
            message.Headers.Add(
                SiPaculBootstrapDefaults
                    .TokenHeaderName,
                bootstrapToken);
        }

        return client.SendAsync(message);
    }

    private static FirstOwnerBootstrapRequest
        CreateRequest()
    {
        return new FirstOwnerBootstrapRequest(
            "SIPACUL",
            "SiPacul Farm",
            "PT SiPacul Lestari",
            "Asia/Jakarta",
            "owner@example.com",
            "ValidPassword!123");
    }

    private static FirstOwnerBootstrapResponse
        CreateBootstrapResponse()
    {
        return new FirstOwnerBootstrapResponse(
            Guid.Parse(
                "10000000-0000-0000-0000-000000000001"),
            "owner@example.com",
            Guid.Parse(
                "20000000-0000-0000-0000-000000000001"),
            "SIPACUL",
            "SiPacul Farm",
            Guid.Parse(
                "30000000-0000-0000-0000-000000000001"),
            OrganizationRole.Owner,
            new DateTime(
                2026,
                8,
                2,
                12,
                0,
                0,
                DateTimeKind.Utc));
    }

    private sealed class BootstrapApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly IFirstOwnerBootstrapService
            _bootstrapService;

        public BootstrapApiFactory(
            IFirstOwnerBootstrapService
                bootstrapService)
        {
            _bootstrapService = bootstrapService;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=localhost;" +
                "Port=5433;" +
                "Database=sipacul_api_tests;" +
                "Username=sipacul_test;" +
                "Password=sipacul_test");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IFirstOwnerBootstrapService>();

                services.AddSingleton(
                    _bootstrapService);
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
                    HandleCookies = true
                });
        }
    }

    private sealed class StubBootstrapService :
        IFirstOwnerBootstrapService
    {
        public FirstOwnerBootstrapStatusResponse Status
        {
            get;
            set;
        } = new(
            true,
            false,
            true);

        public FirstOwnerBootstrapResult Result
        {
            get;
            set;
        } =
            FirstOwnerBootstrapResult.Failed(
                FirstOwnerBootstrapFailure
                    .PersistenceFailure);

        public int StatusCallCount
        {
            get;
            private set;
        }

        public int BootstrapCallCount
        {
            get;
            private set;
        }

        public FirstOwnerBootstrapRequest? LastRequest
        {
            get;
            private set;
        }

        public string? LastSuppliedToken
        {
            get;
            private set;
        }

        public Task<FirstOwnerBootstrapStatusResponse>
            GetStatusAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            StatusCallCount++;

            return Task.FromResult(Status);
        }

        public Task<FirstOwnerBootstrapResult>
            BootstrapAsync(
                FirstOwnerBootstrapRequest request,
                string? suppliedToken,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            BootstrapCallCount++;
            LastRequest = request;
            LastSuppliedToken = suppliedToken;

            return Task.FromResult(Result);
        }
    }
}
