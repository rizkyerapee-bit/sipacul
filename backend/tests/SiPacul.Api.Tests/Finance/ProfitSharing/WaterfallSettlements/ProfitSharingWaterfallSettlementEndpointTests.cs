using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Mappings;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Shared.Results;

namespace SiPacul.Api.Tests.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallSettlementEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Finalize_ShouldCreateAndRequireFinalizePermission()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            new FinalizeProfitSharingWaterfallSettlementRequest(
                "SET-001",
                new DateOnly(2027, 7, 24),
                null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, service.FinalizeCallCount);
        Assert.Equal(
            Permissions.ProfitSharingFinalize,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Finalize_WhenActiveSettlementExists_ShouldConflict()
    {
        var service = new StubService
        {
            FinalizeResult =
                Result<ProfitSharingWaterfallSettlementResponse>.Failure(
                    ProfitSharingWaterfallSettlementErrors
                        .ActiveSettlementExists(CropCycleId))
        };
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            new FinalizeProfitSharingWaterfallSettlementRequest(
                "SET-002",
                new DateOnly(2027, 7, 24),
                null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkAndRequireReadPermission()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.GetAllCallCount);
        Assert.Equal(
            Permissions.ProfitSharingRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task GetById_ShouldReturnPersistedSnapshot()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{service.Response.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.GetByIdCallCount);
    }

    [Fact]
    public async Task Void_ShouldRequireVoidPermission()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{service.Response.Id}/void",
            new VoidProfitSharingWaterfallSettlementRequest(
                "Koreksi sumber."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.VoidCallCount);
        Assert.Equal(
            Permissions.ProfitSharingVoid,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Finalize_WhenForbidden_ShouldNotCallService()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            new FinalizeProfitSharingWaterfallSettlementRequest(
                "SET-003",
                new DateOnly(2027, 7, 24),
                null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.FinalizeCallCount);
    }

    private static string BasePath =>
        "/api/v1/organizations/" +
        $"{OrganizationId}/crop-cycles/{CropCycleId}/" +
        "profit-sharing-waterfall-settlements";

    private static ProfitSharingWaterfallSettlementResponse
        CreateResponse()
    {
        var participant = new ProfitSharingSchemeParticipantDefinition(
            "PERUSAHAAN",
            "Perusahaan",
            ProfitSharingParticipantRole.Company,
            true,
            1);

        var scheme = ProfitSharingScheme.CreateDraft(
            OrganizationId,
            "INTERNAL",
            "Internal Perusahaan",
            null,
            [participant],
            [],
            ProfitSharingResidualMethod.RemainderToParticipant,
            "PERUSAHAAN",
            []);
        scheme.Activate();

        var assignment = ProfitSharingSchemeAssignment.Create(
            OrganizationId,
            CropCycleId,
            scheme);

        var now = new DateTime(
            2027,
            7,
            24,
            8,
            0,
            0,
            DateTimeKind.Utc);

        var profitability = CropCycleProfitabilityReport.Calculate(
            new CropCycleProfitabilityInput(
                OrganizationId,
                CropCycleId,
                "CYCLE-001",
                "Siklus Uji",
                Guid.NewGuid(),
                "CABAI",
                "Cabai",
                150_000m,
                150_000m,
                100_000m,
                0m,
                100_000m,
                0m,
                0m,
                now));

        var calculation = ProfitSharingWaterfallCalculator.Calculate(
            profitability,
            new ProfitSharingWaterfallSchemeInput(
                [
                    new ProfitSharingWaterfallParticipantInput(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        100_000m,
                        true,
                        1)
                ],
                [],
                ProfitSharingResidualPolicyInput
                    .RemainderToParticipant("PERUSAHAAN")));

        return ProfitSharingWaterfallSettlement.CreateFinalized(
                OrganizationId,
                CropCycleId,
                "SET-001",
                new DateOnly(2027, 7, 24),
                assignment,
                profitability,
                calculation,
                null,
                now.AddHours(1))
            .ToResponse();
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly IProfitSharingWaterfallSettlementService
            _service;

        public ApiFactory(
            IProfitSharingWaterfallSettlementService service)
        {
            _service = service;
        }

        public ConfigurableOrganizationPermissionService Authorization
        {
            get;
        } = new();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddOrganizationAuthorizationForTests(
                    Authorization);
                services.RemoveAll<
                    IProfitSharingWaterfallSettlementService>();
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
        IProfitSharingWaterfallSettlementService
    {
        public ProfitSharingWaterfallSettlementResponse Response
        {
            get;
        } = CreateResponse();

        public Result<ProfitSharingWaterfallSettlementResponse>
            FinalizeResult { get; set; }

        public int FinalizeCallCount { get; private set; }

        public int GetAllCallCount { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public int VoidCallCount { get; private set; }

        public StubService()
        {
            FinalizeResult =
                Result<ProfitSharingWaterfallSettlementResponse>
                    .Success(Response);
        }

        public Task<Result<ProfitSharingWaterfallSettlementResponse>>
            FinalizeAsync(
                Guid organizationId,
                Guid cropCycleId,
                FinalizeProfitSharingWaterfallSettlementRequest request,
                CancellationToken cancellationToken = default)
        {
            FinalizeCallCount++;
            return Task.FromResult(FinalizeResult);
        }

        public Task<Result<
            IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                ProfitSharingWaterfallSettlementFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            return Task.FromResult(
                Result<
                    IReadOnlyList<
                        ProfitSharingWaterfallSettlementResponse>>
                    .Success([Response]));
        }

        public Task<Result<ProfitSharingWaterfallSettlementResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(
                Result<ProfitSharingWaterfallSettlementResponse>
                    .Success(Response));
        }

        public Task<Result<ProfitSharingWaterfallSettlementResponse>>
            VoidAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                VoidProfitSharingWaterfallSettlementRequest request,
                CancellationToken cancellationToken = default)
        {
            VoidCallCount++;
            return Task.FromResult(
                Result<ProfitSharingWaterfallSettlementResponse>
                    .Success(Response));
        }
    }
}
