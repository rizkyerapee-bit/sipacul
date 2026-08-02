using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Finance.ProfitSharing;
using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Services;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.ProfitSharing;

public sealed class
    ProfitSharingSettlementEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid SettlementId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly Guid AllocationId =
        Guid.Parse(
            "40000000-0000-0000-0000-000000000001");

    private static readonly DateOnly SettlementDate =
        new(2027, 5, 20);

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

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    ProfitSharingSettlementResponse>();

        Assert.NotNull(body);
        Assert.Equal(SettlementId, body!.Id);

        Assert.Equal(
            $"{BasePath}/{SettlementId}",
            response.Headers.Location?.AbsolutePath);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(
            CropCycleId,
            service.LastCropCycleId);

        Assert.Equal(request, service.LastCreateRequest);
    }

    [Fact]
    public async Task CreateDraft_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSettlementResponse>
                    .Failure(
                        ProfitSharingSettlementErrors
                            .Validation(
                                "Invalid settlement."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateDraft_WhenCropCycleMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSettlementResponse>
                    .Failure(
                        ProfitSharingSettlementErrors
                            .CropCycleNotFound(
                                CropCycleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateDraft_WhenCodeExists_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSettlementResponse>
                    .Failure(
                        ProfitSharingSettlementErrors
                            .CodeAlreadyExists(
                                "SET-001"))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldBindFiltersAndReturnOk()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var path =
            BasePath +
            "?status=Finalized" +
            "&settlementDateFrom=2027-05-01" +
            "&settlementDateTo=2027-05-31" +
            "&managingPartnerCode=MITRA-001";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            ProfitSharingSettlementStatus.Finalized,
            service.LastFilter!.Status);

        Assert.Equal(
            new DateOnly(2027, 5, 1),
            service.LastFilter.SettlementDateFrom);

        Assert.Equal(
            new DateOnly(2027, 5, 31),
            service.LastFilter.SettlementDateTo);

        Assert.Equal(
            "MITRA-001",
            service.LastFilter.ManagingPartnerCode);
    }

    [Fact]
    public async Task GetAll_WithInvalidEnum_ShouldReturnBadRequest()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath + "?status=Unsupported");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(0, service.GetAllCallCount);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{SettlementId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            SettlementId,
            service.LastSettlementId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSettlementResponse>
                    .Failure(
                        ProfitSharingSettlementErrors
                            .NotFound(
                                SettlementId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{SettlementId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_ShouldBindRequestAndReturnOk()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new UpdateProfitSharingSettlementRequest(
                SettlementDate.AddDays(1),
                "Updated");

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{SettlementId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastUpdateRequest);

        Assert.Equal(
            SettlementId,
            service.LastSettlementId);
    }

    [Fact]
    public async Task Finalize_ShouldReturnOk()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSettlementResponse>
                    .Success(
                        CreateResponse(
                            ProfitSharingSettlementStatus
                                .Finalized))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{SettlementId}/finalize",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(1, service.FinalizeCallCount);
        Assert.Equal(SettlementId, service.LastSettlementId);
    }

    [Fact]
    public async Task Finalize_WhenSourcesChanged_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<ProfitSharingSettlementResponse>
                    .Failure(
                        ProfitSharingSettlementErrors
                            .SourceDataChanged())
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{SettlementId}/finalize",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Void_ShouldBindRequestAndReturnOk()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new VoidProfitSharingSettlementRequest(
                "Koreksi sumber");

        var message =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"{BasePath}/{SettlementId}/void")
            {
                Content =
                    JsonContent.Create(request)
            };

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastVoidRequest);

        Assert.Equal(
            SettlementId,
            service.LastSettlementId);
    }

    [Fact]
    public async Task InvalidGuidRoute_ShouldReturnNotFound()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/v1/organizations/not-a-guid/" +
            $"crop-cycles/{CropCycleId}/" +
            "profit-sharing-settlements");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static string BasePath =>
        "/api/v1/organizations/" +
        $"{OrganizationId}/crop-cycles/" +
        $"{CropCycleId}/profit-sharing-settlements";

    private static
        CreateProfitSharingSettlementRequest
        CreateRequest()
    {
        return new CreateProfitSharingSettlementRequest(
            "SET-001",
            SettlementDate,
            "MITRA-001",
            "Mitra Pengelola",
            "Catatan");
    }

    private static ProfitSharingSettlementResponse
        CreateResponse(
            ProfitSharingSettlementStatus status =
                ProfitSharingSettlementStatus.Draft)
    {
        var allocation =
            new ProfitSharingAllocationResponse(
                AllocationId,
                OrganizationId,
                SettlementId,
                "INV-001",
                "Investor Utama",
                CapitalContributorRole.Investor,
                300,
                1,
                300,
                0,
                0,
                200,
                200,
                500,
                1,
                new DateTime(
                    2027,
                    5,
                    20,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc));

        return new ProfitSharingSettlementResponse(
            SettlementId,
            OrganizationId,
            CropCycleId,
            "SET-001",
            SettlementDate,
            "MITRA-001",
            "Mitra Pengelola",
            500,
            500,
            0,
            200,
            100,
            300,
            200,
            ProfitabilityOutcome.Profit,
            66.67m,
            133.33m,
            300,
            0,
            300,
            300,
            0,
            133.33m,
            66.67m,
            500,
            ProfitSharingCalculator
                .CurrentCalculationVersion,
            "Catatan",
            status,
            status ==
                ProfitSharingSettlementStatus.Finalized,
            status ==
                ProfitSharingSettlementStatus.Finalized
                ? new DateTime(
                    2027,
                    5,
                    20,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc)
                : null,
            null,
            null,
            new DateTime(
                2027,
                5,
                20,
                8,
                0,
                0,
                DateTimeKind.Utc),
            null,
            [allocation]);
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly
            IProfitSharingSettlementService _service;

        public ApiFactory(
            IProfitSharingSettlementService service)
        {
            _service = service;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IProfitSharingSettlementService>();

                services.AddSingleton(_service);
            });
        }

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("https://localhost")
                });
        }
    }

    private sealed class StubService :
        IProfitSharingSettlementService
    {
        public Result<ProfitSharingSettlementResponse>
            SingleResult
        {
            get;
            set;
        } = Result<ProfitSharingSettlementResponse>
            .Success(CreateResponse());

        public Result<
            IReadOnlyList<
                ProfitSharingSettlementResponse>>
            ListResult
        {
            get;
            set;
        } = Result<
                IReadOnlyList<
                    ProfitSharingSettlementResponse>>
            .Success([CreateResponse()]);

        public Guid LastOrganizationId
        {
            get;
            private set;
        }

        public Guid LastCropCycleId
        {
            get;
            private set;
        }

        public Guid LastSettlementId
        {
            get;
            private set;
        }

        public CreateProfitSharingSettlementRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateProfitSharingSettlementRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public VoidProfitSharingSettlementRequest?
            LastVoidRequest
        {
            get;
            private set;
        }

        public ProfitSharingSettlementFilter? LastFilter
        {
            get;
            private set;
        }

        public int GetAllCallCount
        {
            get;
            private set;
        }

        public int FinalizeCallCount
        {
            get;
            private set;
        }

        public Task<Result<ProfitSharingSettlementResponse>>
            CreateDraftAsync(
                Guid organizationId,
                Guid cropCycleId,
                CreateProfitSharingSettlementRequest request,
                CancellationToken cancellationToken = default)
        {
            Record(
                organizationId,
                cropCycleId);

            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<
            Result<
                IReadOnlyList<
                    ProfitSharingSettlementResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                ProfitSharingSettlementFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Record(
                organizationId,
                cropCycleId);

            LastFilter = filter;
            GetAllCallCount++;

            return Task.FromResult(ListResult);
        }

        public Task<Result<ProfitSharingSettlementResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                CancellationToken cancellationToken = default)
        {
            Record(
                organizationId,
                cropCycleId,
                settlementId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<ProfitSharingSettlementResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                UpdateProfitSharingSettlementRequest request,
                CancellationToken cancellationToken = default)
        {
            Record(
                organizationId,
                cropCycleId,
                settlementId);

            LastUpdateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<ProfitSharingSettlementResponse>>
            FinalizeAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                CancellationToken cancellationToken = default)
        {
            Record(
                organizationId,
                cropCycleId,
                settlementId);

            FinalizeCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<ProfitSharingSettlementResponse>>
            VoidAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid settlementId,
                VoidProfitSharingSettlementRequest request,
                CancellationToken cancellationToken = default)
        {
            Record(
                organizationId,
                cropCycleId,
                settlementId);

            LastVoidRequest = request;

            return Task.FromResult(SingleResult);
        }

        private void Record(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId = default)
        {
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            LastSettlementId = settlementId;
        }
    }
}
