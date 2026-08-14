using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Schemes;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemeEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid SchemeId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task CreateDraft_ShouldReturnCreatedAndLocation()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();
        var request = CreateRequest();

        var response = await client.PostAsJsonAsync(
            BasePath,
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            $"{BasePath}/{SchemeId}",
            response.Headers.Location?.AbsolutePath);
        Assert.NotNull(service.LastCreateRequest);
        Assert.Equal(
            request.Code,
            service.LastCreateRequest!.Code);
        Assert.Equal(
            request.ResidualMethod,
            service.LastCreateRequest.ResidualMethod);
        Assert.Single(service.LastCreateRequest.Participants);
        Assert.Equal(
            Permissions.ProfitSharingWrite,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task GetAll_ShouldBindStatusAndCode()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath + "?status=Active&code=SCHEME-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.LastFilter);
        Assert.Equal(
            ProfitSharingSchemeStatus.Active,
            service.LastFilter!.Status);
        Assert.Equal("SCHEME-001", service.LastFilter.Code);
        Assert.Equal(
            Permissions.ProfitSharingRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task UpdateDraft_ShouldBindRequest()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();
        var request = UpdateRequest();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{SchemeId}",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.LastUpdateRequest);
        Assert.Equal(
            request.Name,
            service.LastUpdateRequest!.Name);
        Assert.Equal(
            request.ResidualRecipientCode,
            service.LastUpdateRequest.ResidualRecipientCode);
        Assert.Equal(SchemeId, service.LastSchemeId);
    }

    [Fact]
    public async Task CreateNextVersion_ShouldReturnCreated()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync(
            $"{BasePath}/{SchemeId}/versions",
            null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, service.CreateNextVersionCallCount);
        Assert.Equal(SchemeId, service.LastSchemeId);
    }

    [Fact]
    public async Task Activate_ShouldRequireFinalizePermission()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{SchemeId}/activate",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.ActivateCallCount);
        Assert.Equal(
            Permissions.ProfitSharingFinalize,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Activate_WhenStatusInvalid_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSchemeResponse>.Failure(
                    ProfitSharingSchemeErrors
                        .InvalidStatusTransition(
                            "Invalid status."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{SchemeId}/activate",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateDraft_WhenForbidden_ShouldNotCallService()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.CreateCallCount);
    }

    private static string BasePath =>
        "/api/v1/organizations/" +
        $"{OrganizationId}/profit-sharing-schemes";

    private static CreateProfitSharingSchemeRequest CreateRequest()
    {
        return new CreateProfitSharingSchemeRequest(
            "SCHEME-001",
            "Skema Utama",
            null,
            [
                new ProfitSharingSchemeParticipantRequest(
                    "PERUSAHAAN",
                    "Perusahaan",
                    ProfitSharingParticipantRole.Company,
                    true,
                    1)
            ],
            [],
            ProfitSharingResidualMethod.RemainderToParticipant,
            "PERUSAHAAN",
            []);
    }

    private static UpdateProfitSharingSchemeDraftRequest
        UpdateRequest()
    {
        var create = CreateRequest();

        return new UpdateProfitSharingSchemeDraftRequest(
            "Skema Revisi",
            null,
            create.Participants,
            create.PriorityRules,
            create.ResidualMethod,
            create.ResidualRecipientCode,
            create.ResidualShares);
    }

    private static ProfitSharingSchemeResponse CreateResponse()
    {
        var createdAt =
            new DateTime(
                2027,
                1,
                1,
                8,
                0,
                0,
                DateTimeKind.Utc);

        return new ProfitSharingSchemeResponse(
            SchemeId,
            OrganizationId,
            SchemeId,
            "SCHEME-001",
            "Skema Utama",
            null,
            1,
            ProfitSharingSchemeStatus.Draft,
            ProfitSharingResidualMethod.RemainderToParticipant,
            "PERUSAHAAN",
            null,
            null,
            createdAt,
            null,
            [
                new ProfitSharingSchemeParticipantResponse(
                    Guid.NewGuid(),
                    "PERUSAHAAN",
                    "Perusahaan",
                    ProfitSharingParticipantRole.Company,
                    true,
                    1)
            ],
            [],
            []);
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly IProfitSharingSchemeService _service;

        public ApiFactory(IProfitSharingSchemeService service)
        {
            _service = service;
        }

        public ConfigurableOrganizationPermissionService
            Authorization { get; } = new();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddOrganizationAuthorizationForTests(
                    Authorization);

                services.RemoveAll<IProfitSharingSchemeService>();
                services.AddSingleton(_service);
            });
        }

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });
        }
    }

    private sealed class StubService :
        IProfitSharingSchemeService
    {
        public Result<ProfitSharingSchemeResponse> SingleResult
        {
            get;
            set;
        } = Result<ProfitSharingSchemeResponse>.Success(
            CreateResponse());

        public Result<IReadOnlyList<ProfitSharingSchemeResponse>>
            ListResult
        {
            get;
            set;
        } = Result<IReadOnlyList<ProfitSharingSchemeResponse>>
            .Success([CreateResponse()]);

        public Guid LastOrganizationId { get; private set; }

        public Guid LastSchemeId { get; private set; }

        public CreateProfitSharingSchemeRequest?
            LastCreateRequest { get; private set; }

        public UpdateProfitSharingSchemeDraftRequest?
            LastUpdateRequest { get; private set; }

        public ProfitSharingSchemeFilter?
            LastFilter { get; private set; }

        public int CreateCallCount { get; private set; }

        public int CreateNextVersionCallCount
        {
            get;
            private set;
        }

        public int ActivateCallCount { get; private set; }

        public Task<Result<ProfitSharingSchemeResponse>>
            CreateDraftAsync(
                Guid organizationId,
                CreateProfitSharingSchemeRequest request,
                CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastCreateRequest = request;
            CreateCallCount++;
            return Task.FromResult(SingleResult);
        }

        public Task<
            Result<IReadOnlyList<ProfitSharingSchemeResponse>>>
            GetAllAsync(
                Guid organizationId,
                ProfitSharingSchemeFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastFilter = filter;
            return Task.FromResult(ListResult);
        }

        public Task<Result<ProfitSharingSchemeResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid schemeId,
                CancellationToken cancellationToken = default)
        {
            Record(organizationId, schemeId);
            return Task.FromResult(SingleResult);
        }

        public Task<Result<ProfitSharingSchemeResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid schemeId,
                UpdateProfitSharingSchemeDraftRequest request,
                CancellationToken cancellationToken = default)
        {
            Record(organizationId, schemeId);
            LastUpdateRequest = request;
            return Task.FromResult(SingleResult);
        }

        public Task<Result<ProfitSharingSchemeResponse>>
            CreateNextVersionAsync(
                Guid organizationId,
                Guid sourceSchemeId,
                CancellationToken cancellationToken = default)
        {
            Record(organizationId, sourceSchemeId);
            CreateNextVersionCallCount++;
            return Task.FromResult(SingleResult);
        }

        public Task<Result<ProfitSharingSchemeResponse>>
            ActivateAsync(
                Guid organizationId,
                Guid schemeId,
                CancellationToken cancellationToken = default)
        {
            Record(organizationId, schemeId);
            ActivateCallCount++;
            return Task.FromResult(SingleResult);
        }

        private void Record(
            Guid organizationId,
            Guid schemeId)
        {
            LastOrganizationId = organizationId;
            LastSchemeId = schemeId;
        }
    }
}
