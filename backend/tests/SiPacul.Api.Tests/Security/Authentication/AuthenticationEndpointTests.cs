using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SiPacul.Api.Security;
using SiPacul.Application.Security.Authentication;
using SiPacul.Application.Security.Authentication.Contracts;
using SiPacul.Application.Security.Authentication.Services;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Api.Tests.Security.Authentication;

public sealed class AuthenticationEndpointTests
{
    private const string CsrfPath =
        "/api/v1/auth/csrf";

    private const string LoginPath =
        "/api/v1/auth/login";

    private const string LogoutPath =
        "/api/v1/auth/logout";

    private const string MePath =
        "/api/v1/auth/me";

    [Fact]
    public void AuthenticationDefaults_ShouldUseIdentityScheme()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        var options =
            factory.Services
                .GetRequiredService<
                    IOptions<AuthenticationOptions>>()
                .Value;

        Assert.Equal(
            IdentityConstants.ApplicationScheme,
            options.DefaultAuthenticateScheme);

        Assert.Equal(
            IdentityConstants.ApplicationScheme,
            options.DefaultChallengeScheme);

        Assert.Equal(
            IdentityConstants.ApplicationScheme,
            options.DefaultSignInScheme);
    }

    [Fact]
    public void Cookie_ShouldUseSecureApiConfiguration()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        var options =
            factory.Services
                .GetRequiredService<
                    IOptionsMonitor<
                        CookieAuthenticationOptions>>()
                .Get(
                    IdentityConstants.ApplicationScheme);

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .AuthenticationCookieName,
            options.Cookie.Name);

        Assert.True(options.Cookie.HttpOnly);

        Assert.Equal(
            CookieSecurePolicy.Always,
            options.Cookie.SecurePolicy);

        Assert.Equal(
            SameSiteMode.Lax,
            options.Cookie.SameSite);

        Assert.Equal("/", options.Cookie.Path);
        Assert.True(options.Cookie.IsEssential);
        Assert.True(options.SlidingExpiration);

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .CookieLifetime,
            options.ExpireTimeSpan);

        Assert.Equal(
            typeof(ApplicationCookieEvents),
            options.EventsType);
    }

    [Fact]
    public void IdentityPasswordPolicy_ShouldMeetBaseline()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        var options =
            factory.Services
                .GetRequiredService<
                    IOptions<IdentityOptions>>()
                .Value;

        Assert.Equal(
            12,
            options.Password.RequiredLength);

        Assert.Equal(
            1,
            options.Password.RequiredUniqueChars);

        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(
            options.Password.RequireNonAlphanumeric);
        Assert.True(options.Password.RequireUppercase);
        Assert.True(options.User.RequireUniqueEmail);
        Assert.False(
            options.SignIn.RequireConfirmedEmail);
    }

    [Fact]
    public void IdentityLockout_ShouldUseConfiguredBaseline()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        var options =
            factory.Services
                .GetRequiredService<
                    IOptions<IdentityOptions>>()
                .Value;

        Assert.True(
            options.Lockout.AllowedForNewUsers);

        Assert.Equal(
            5,
            options.Lockout.MaxFailedAccessAttempts);

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .LockoutDuration,
            options.Lockout.DefaultLockoutTimeSpan);

        var stampOptions =
            factory.Services
                .GetRequiredService<
                    IOptions<
                        SecurityStampValidatorOptions>>()
                .Value;

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .SecurityStampValidationInterval,
            stampOptions.ValidationInterval);
    }

    [Fact]
    public void Antiforgery_ShouldUseSecureHeaderConfiguration()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        var options =
            factory.Services
                .GetRequiredService<
                    IOptions<AntiforgeryOptions>>()
                .Value;

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .AntiforgeryHeaderName,
            options.HeaderName);

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .AntiforgeryCookieName,
            options.Cookie.Name);

        Assert.True(options.Cookie.HttpOnly);

        Assert.Equal(
            CookieSecurePolicy.Always,
            options.Cookie.SecurePolicy);

        Assert.Equal(
            SameSiteMode.Lax,
            options.Cookie.SameSite);

        Assert.Equal("/", options.Cookie.Path);
        Assert.True(options.Cookie.IsEssential);
    }

    [Fact]
    public async Task Csrf_ShouldReturnTokenAndNoStoreHeaders()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(CsrfPath);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    AntiforgeryTokenResponse>();

        Assert.NotNull(body);
        Assert.False(
            string.IsNullOrWhiteSpace(
                body!.RequestToken));

        Assert.Equal(
            SiPaculAuthenticationDefaults
                .AntiforgeryHeaderName,
            body.HeaderName);

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

        Assert.True(
            response.Headers.TryGetValues(
                "Set-Cookie",
                out var cookies));

        Assert.Contains(
            cookies,
            value =>
                value.Contains(
                    SiPaculAuthenticationDefaults
                        .AntiforgeryCookieName,
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_WithValidToken_ShouldReturnCurrentUser()
    {
        var expectedUser = CreateCurrentUser();

        var service =
            new StubAuthenticationService
            {
                LoginResult =
                    UserAuthenticationResult.Success(
                        expectedUser)
            };

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var token =
            await GetAntiforgeryTokenAsync(client);

        var response =
            await SendLoginAsync(
                client,
                new LoginRequest(
                    "  owner@example.com  ",
                    "ValidPassword!123",
                    true),
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    CurrentUserResponse>();

        Assert.NotNull(body);
        Assert.Equal(expectedUser.UserId, body!.UserId);
        Assert.Equal(expectedUser.Email, body.Email);
        Assert.Equal(
            expectedUser.EmailConfirmed,
            body.EmailConfirmed);
        Assert.Equal(
            expectedUser.LastLoginAt,
            body.LastLoginAt);

        Assert.Single(body.Memberships);

        var expectedMembership =
            Assert.Single(expectedUser.Memberships);

        var actualMembership =
            Assert.Single(body.Memberships);

        Assert.Equal(
            expectedMembership.MembershipId,
            actualMembership.MembershipId);

        Assert.Equal(
            expectedMembership.OrganizationId,
            actualMembership.OrganizationId);

        Assert.Equal(
            expectedMembership.Role,
            actualMembership.Role);

        Assert.Equal(
            expectedMembership.Permissions,
            actualMembership.Permissions);

        Assert.Equal(1, service.LoginCallCount);
        Assert.NotNull(service.LastLoginRequest);

        Assert.Equal(
            "owner@example.com",
            service.LastLoginRequest!.Email);

        Assert.True(
            service.LastLoginRequest.RememberMe);
    }

    [Fact]
    public async Task Login_WithoutAntiforgeryToken_ShouldReturnBadRequest()
    {
        var service =
            new StubAuthenticationService();

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var response =
            await client.PostAsJsonAsync(
                LoginPath,
                new LoginRequest(
                    "owner@example.com",
                    "ValidPassword!123",
                    false));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            AuthenticationErrorCodes
                .InvalidAntiforgeryToken,
            content);

        Assert.Equal(0, service.LoginCallCount);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldBeGeneric()
    {
        var service =
            new StubAuthenticationService
            {
                LoginResult =
                    UserAuthenticationResult.Failed()
            };

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var token =
            await GetAntiforgeryTokenAsync(client);

        var response =
            await SendLoginAsync(
                client,
                new LoginRequest(
                    "owner@example.com",
                    "WrongPassword!123",
                    false),
                token);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            AuthenticationErrorCodes
                .InvalidCredentials,
            content);

        Assert.DoesNotContain(
            "owner@example.com",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(1, service.LoginCallCount);
    }

    [Fact]
    public async Task Login_WithBlankEmail_ShouldReturnBadRequest()
    {
        var service =
            new StubAuthenticationService();

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var token =
            await GetAntiforgeryTokenAsync(client);

        var response =
            await SendLoginAsync(
                client,
                new LoginRequest(
                    " ",
                    "ValidPassword!123",
                    false),
                token);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(0, service.LoginCallCount);
    }

    [Fact]
    public async Task Login_WithBlankPassword_ShouldReturnBadRequest()
    {
        var service =
            new StubAuthenticationService();

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var token =
            await GetAntiforgeryTokenAsync(client);

        var response =
            await SendLoginAsync(
                client,
                new LoginRequest(
                    "owner@example.com",
                    " ",
                    false),
                token);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(0, service.LoginCallCount);
    }

    [Fact]
    public async Task Me_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var service =
            new StubAuthenticationService();

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(MePath);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            0,
            service.GetCurrentUserCallCount);
    }

    [Fact]
    public async Task Logout_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var service =
            new StubAuthenticationService();

        using var factory =
            new AuthenticationApiFactory(service);

        using var client = factory.CreateHttpsClient();

        var response =
            await client.PostAsync(
                LogoutPath,
                null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(0, service.SignOutCallCount);
    }

    [Fact]
    public async Task UnknownAuthenticationRoute_ShouldReturnNotFound()
    {
        using var factory =
            new AuthenticationApiFactory(
                new StubAuthenticationService());

        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(
                "/api/v1/auth/unsupported");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static async Task<string>
        GetAntiforgeryTokenAsync(
            HttpClient client)
    {
        var response =
            await client.GetAsync(CsrfPath);

        response.EnsureSuccessStatusCode();

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    AntiforgeryTokenResponse>();

        Assert.NotNull(body);

        return body!.RequestToken;
    }

    private static Task<HttpResponseMessage>
        SendLoginAsync(
            HttpClient client,
            LoginRequest request,
            string antiforgeryToken)
    {
        var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                LoginPath)
            {
                Content = JsonContent.Create(request)
            };

        message.Headers.Add(
            SiPaculAuthenticationDefaults
                .AntiforgeryHeaderName,
            antiforgeryToken);

        return client.SendAsync(message);
    }

    private static CurrentUserResponse
        CreateCurrentUser()
    {
        return new CurrentUserResponse(
            Guid.Parse(
                "10000000-0000-0000-0000-000000000001"),
            "owner@example.com",
            false,
            new DateTime(
                2026,
                8,
                2,
                10,
                0,
                0,
                DateTimeKind.Utc),
            new[]
            {
                new CurrentUserMembershipResponse(
                    Guid.Parse(
                        "20000000-0000-0000-0000-000000000001"),
                    Guid.Parse(
                        "30000000-0000-0000-0000-000000000001"),
                    OrganizationRole.Owner,
                    new[]
                    {
                        "organizations.read"
                    })
            });
    }

    private sealed class AuthenticationApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly IUserAuthenticationService
            _authenticationService;

        public AuthenticationApiFactory(
            IUserAuthenticationService
                authenticationService)
        {
            _authenticationService =
                authenticationService;
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
                    IUserAuthenticationService>();

                services.AddSingleton(
                    _authenticationService);
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

    private sealed class StubAuthenticationService :
        IUserAuthenticationService
    {
        public UserAuthenticationResult LoginResult
        {
            get;
            set;
        } = UserAuthenticationResult.Failed();

        public CurrentUserResponse? CurrentUser
        {
            get;
            set;
        }

        public int LoginCallCount
        {
            get;
            private set;
        }

        public int SignOutCallCount
        {
            get;
            private set;
        }

        public int GetCurrentUserCallCount
        {
            get;
            private set;
        }

        public LoginRequest? LastLoginRequest
        {
            get;
            private set;
        }

        public Task<UserAuthenticationResult> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LoginCallCount++;
            LastLoginRequest = request;

            return Task.FromResult(LoginResult);
        }

        public Task SignOutAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            SignOutCallCount++;

            return Task.CompletedTask;
        }

        public Task<CurrentUserResponse?>
            GetCurrentUserAsync(
                System.Security.Claims.ClaimsPrincipal
                    principal,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            GetCurrentUserCallCount++;

            return Task.FromResult(CurrentUser);
        }
    }
}
