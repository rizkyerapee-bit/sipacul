using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Assignments;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.ProfitSharing.Assignments;

public sealed class ProfitSharingSchemeAssignmentEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid SchemeId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Get_ShouldReturnSnapshotAndRequireRead()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.GetCallCount);
        Assert.Equal(OrganizationId, service.LastOrganizationId);
        Assert.Equal(CropCycleId, service.LastCropCycleId);
        Assert.Equal(
            Permissions.ProfitSharingRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Assign_ShouldBindSchemeAndRequireWrite()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            BasePath,
            new AssignProfitSharingSchemeRequest(SchemeId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.AssignCallCount);
        Assert.Equal(
            SchemeId,
            service.LastRequest?.SchemeId);
        Assert.Equal(
            Permissions.ProfitSharingWrite,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Assign_WhenLocked_ShouldReturnConflict()
    {
        var service = new StubService
        {
            Result =
                Result<ProfitSharingSchemeAssignmentResponse>.Failure(
                    ProfitSharingSchemeAssignmentErrors
                        .AssignmentLocked(CropCycleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            BasePath,
            new AssignProfitSharingSchemeRequest(SchemeId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            Result =
                Result<ProfitSharingSchemeAssignmentResponse>.Failure(
                    ProfitSharingSchemeAssignmentErrors
                        .AssignmentNotFound(CropCycleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_WhenForbidden_ShouldNotCallService()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            BasePath,
            new AssignProfitSharingSchemeRequest(SchemeId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.AssignCallCount);
    }

    private static string BasePath =>
        "/api/v1/organizations/" +
        $"{OrganizationId}/crop-cycles/{CropCycleId}/" +
        "profit-sharing-scheme";

    private static ProfitSharingSchemeAssignmentResponse
        CreateResponse()
    {
        var now =
            new DateTime(
                2027,
                1,
                1,
                8,
                0,
                0,
                DateTimeKind.Utc);

        return new ProfitSharingSchemeAssignmentResponse(
            Guid.Parse(
                "40000000-0000-0000-0000-000000000001"),
            OrganizationId,
            CropCycleId,
            SchemeId,
            SchemeId,
            "SCHEME-001",
            "Skema Utama",
            null,
            1,
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            now,
            now,
            null,
            [
                new ProfitSharingSchemeAssignmentParticipantResponse(
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
        private readonly IProfitSharingSchemeAssignmentService
            _service;

        public ApiFactory(
            IProfitSharingSchemeAssignmentService service)
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

                services.RemoveAll<
                    IProfitSharingSchemeAssignmentService>();
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
        IProfitSharingSchemeAssignmentService
    {
        public Result<ProfitSharingSchemeAssignmentResponse> Result
        {
            get;
            set;
        } = Result<ProfitSharingSchemeAssignmentResponse>.Success(
            CreateResponse());

        public Guid LastOrganizationId { get; private set; }

        public Guid LastCropCycleId { get; private set; }

        public AssignProfitSharingSchemeRequest? LastRequest
        {
            get;
            private set;
        }

        public int AssignCallCount { get; private set; }

        public int GetCallCount { get; private set; }

        public Task<Result<ProfitSharingSchemeAssignmentResponse>>
            AssignAsync(
                Guid organizationId,
                Guid cropCycleId,
                AssignProfitSharingSchemeRequest request,
                CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            LastRequest = request;
            AssignCallCount++;
            return Task.FromResult(Result);
        }

        public Task<Result<ProfitSharingSchemeAssignmentResponse>>
            GetAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            GetCallCount++;
            return Task.FromResult(Result);
        }
    }
}
