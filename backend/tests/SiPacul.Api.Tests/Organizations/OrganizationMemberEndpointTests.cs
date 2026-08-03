using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Security;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Organizations.Members;
using SiPacul.Application.Organizations.Members.Contracts;
using SiPacul.Application.Organizations.Members.Services;
using SiPacul.Application.Security.Authentication.Contracts;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;

namespace SiPacul.Api.Tests.Organizations;

public sealed class OrganizationMemberEndpointTests
{
    private const string CsrfPath =
        "/api/v1/auth/csrf";

    [Fact]
    public async Task GetAll_WhenSuccessful_ShouldReturnMembers()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var member = CreateMemberResponse();

        factory.Service.GetAllResult = Result<
            IReadOnlyList<OrganizationMemberResponse>>
            .Success(new[] { member });

        using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync(
            MembersPath(organizationId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<OrganizationMemberResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(member.MembershipId, content![0].MembershipId);
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ShouldReturnMember()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var member = CreateMemberResponse();

        factory.Service.GetByIdResult =
            Result<OrganizationMemberResponse>.Success(member);

        using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync(
            MemberPath(
                organizationId,
                member.MembershipId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<OrganizationMemberResponse>();

        Assert.NotNull(content);
        Assert.Equal(member.MembershipId, content!.MembershipId);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var member = CreateMemberResponse();

        factory.Service.CreateResult =
            Result<OrganizationMemberResponse>.Success(member);

        using var client = factory.CreateHttpsClient();
        var request = new CreateOrganizationMemberRequest(
            "operator@example.com",
            "StrongPass123!",
            OrganizationRole.Operator);

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            MembersPath(organizationId))
        {
            Content = JsonContent.Create(request)
        };

        await AddAntiforgeryAsync(client, message);

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<OrganizationMemberResponse>();

        Assert.NotNull(content);
        Assert.Equal(member.MembershipId, content!.MembershipId);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task ChangeRole_WhenSuccessful_ShouldReturnOk()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var member = CreateMemberResponse(
            role: OrganizationRole.Finance);

        factory.Service.ChangeRoleResult =
            Result<OrganizationMemberResponse>.Success(member);

        using var client = factory.CreateHttpsClient();
        var request = new UpdateOrganizationMemberRoleRequest(
            OrganizationRole.Finance);

        using var message = new HttpRequestMessage(
            HttpMethod.Patch,
            MemberPath(organizationId, member.MembershipId) +
                "/role")
        {
            Content = JsonContent.Create(request)
        };

        await AddAntiforgeryAsync(client, message);

        var response = await client.SendAsync(message);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnActive()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var member = CreateMemberResponse(
            status: OrganizationMembershipStatus.Active);

        factory.Service.ActivateResult =
            Result<OrganizationMemberResponse>.Success(member);

        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            MemberPath(organizationId, member.MembershipId) +
                "/activate");

        await AddAntiforgeryAsync(client, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Suspend_WhenSuccessful_ShouldReturnSuspended()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var member = CreateMemberResponse(
            status: OrganizationMembershipStatus.Suspended);

        factory.Service.SuspendResult =
            Result<OrganizationMemberResponse>.Success(member);

        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            MemberPath(organizationId, member.MembershipId) +
                "/suspend");

        await AddAntiforgeryAsync(client, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = new MemberApiFactory();
        using var client = factory.CreateHttpsClient();
        var organizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            MembersPath(organizationId));

        request.Headers.Add(
            OrganizationAuthorizationTestSupport
                .UnauthenticatedHeaderName,
            "true");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutReadPermission_ShouldReturnForbidden()
    {
        using var factory = new MemberApiFactory();
        factory.Authorization.Granted = false;

        using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync(
            MembersPath(Guid.NewGuid()));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.MembersRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WithoutManagePermission_ShouldReturnForbidden()
    {
        using var factory = new MemberApiFactory();
        factory.Authorization.Granted = false;

        using var client = factory.CreateHttpsClient();
        var request = new CreateOrganizationMemberRequest(
            "operator@example.com",
            "StrongPass123!",
            OrganizationRole.Operator);

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            MembersPath(Guid.NewGuid()))
        {
            Content = JsonContent.Create(request)
        };

        await AddAntiforgeryAsync(client, message);

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.MembersManage,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task ChangeRole_ForOwner_ShouldReturnForbidden()
    {
        using var factory = new MemberApiFactory();
        var organizationId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        factory.Service.ChangeRoleResult =
            Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.OwnerProtected());

        using var client = factory.CreateHttpsClient();
        var request = new UpdateOrganizationMemberRoleRequest(
            OrganizationRole.Admin);

        using var message = new HttpRequestMessage(
            HttpMethod.Patch,
            MemberPath(organizationId, membershipId) +
                "/role")
        {
            Content = JsonContent.Create(request)
        };

        await AddAntiforgeryAsync(client, message);

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenInvalid_ShouldReturnBadRequest()
    {
        using var factory = new MemberApiFactory();

        factory.Service.CreateResult =
            Result<OrganizationMemberResponse>.Failure(
                OrganizationMemberErrors.Validation(
                    "Member email is required."));

        using var client = factory.CreateHttpsClient();
        var request = new CreateOrganizationMemberRequest(
            string.Empty,
            "StrongPass123!",
            OrganizationRole.Operator);

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            MembersPath(Guid.NewGuid()))
        {
            Content = JsonContent.Create(request)
        };

        await AddAntiforgeryAsync(client, message);

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAntiforgery_ShouldReturnBadRequest()
    {
        using var factory = new MemberApiFactory();
        using var client = factory.CreateHttpsClient();
        var request = new CreateOrganizationMemberRequest(
            "operator@example.com",
            "StrongPass123!",
            OrganizationRole.Operator);

        var response = await client.PostAsJsonAsync(
            MembersPath(Guid.NewGuid()),
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static string MembersPath(Guid organizationId)
    {
        return $"/api/v1/organizations/{organizationId}/members";
    }

    private static string MemberPath(
        Guid organizationId,
        Guid membershipId)
    {
        return MembersPath(organizationId) +
            $"/{membershipId}";
    }

    private static async Task AddAntiforgeryAsync(
        HttpClient client,
        HttpRequestMessage request)
    {
        var response = await client.GetAsync(CsrfPath);

        response.EnsureSuccessStatusCode();

        var token = await response.Content
            .ReadFromJsonAsync<AntiforgeryTokenResponse>();

        Assert.NotNull(token);

        request.Headers.Add(
            SiPaculAuthenticationDefaults
                .AntiforgeryHeaderName,
            token!.RequestToken);
    }

    private static OrganizationMemberResponse
        CreateMemberResponse(
            OrganizationRole role = OrganizationRole.Operator,
            OrganizationMembershipStatus status =
                OrganizationMembershipStatus.Active)
    {
        return new OrganizationMemberResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "operator@example.com",
            false,
            true,
            role,
            status,
            DateTime.UtcNow,
            status == OrganizationMembershipStatus.Suspended
                ? DateTime.UtcNow
                : null);
    }

    private sealed class MemberApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeOrganizationMemberService Service
        { get; } = new();

        public ConfigurableOrganizationPermissionService
            Authorization
        { get; } = new();

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });
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

            builder.ConfigureServices(services =>
            {
                services.AddOrganizationAuthorizationForTests(
                    Authorization);

                services.RemoveAll<IOrganizationMemberService>();

                services.AddSingleton<IOrganizationMemberService>(
                    Service);
            });
        }
    }

    private sealed class FakeOrganizationMemberService :
        IOrganizationMemberService
    {
        public Result<
            IReadOnlyList<OrganizationMemberResponse>>
            GetAllResult
        { get; set; } = Result<
            IReadOnlyList<OrganizationMemberResponse>>
            .Success(Array.Empty<OrganizationMemberResponse>());

        public Result<OrganizationMemberResponse>
            GetByIdResult
        { get; set; } = SuccessResponse();

        public Result<OrganizationMemberResponse>
            CreateResult
        { get; set; } = SuccessResponse();

        public Result<OrganizationMemberResponse>
            ChangeRoleResult
        { get; set; } = SuccessResponse();

        public Result<OrganizationMemberResponse>
            ActivateResult
        { get; set; } = SuccessResponse();

        public Result<OrganizationMemberResponse>
            SuspendResult
        { get; set; } = SuccessResponse(
            OrganizationMembershipStatus.Suspended);

        public Task<Result<
            IReadOnlyList<OrganizationMemberResponse>>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAllResult);
        }

        public Task<Result<OrganizationMemberResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid membershipId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<OrganizationMemberResponse>>
            CreateAsync(
                Guid organizationId,
                CreateOrganizationMemberRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<Result<OrganizationMemberResponse>>
            ChangeRoleAsync(
                Guid organizationId,
                Guid membershipId,
                UpdateOrganizationMemberRoleRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ChangeRoleResult);
        }

        public Task<Result<OrganizationMemberResponse>>
            ActivateAsync(
                Guid organizationId,
                Guid membershipId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivateResult);
        }

        public Task<Result<OrganizationMemberResponse>>
            SuspendAsync(
                Guid organizationId,
                Guid membershipId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SuspendResult);
        }

        private static Result<OrganizationMemberResponse>
            SuccessResponse(
                OrganizationMembershipStatus status =
                    OrganizationMembershipStatus.Active)
        {
            return Result<OrganizationMemberResponse>.Success(
                CreateMemberResponse(status: status));
        }
    }
}
