using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Harvests;
using SiPacul.Application.Harvests.Contracts;
using SiPacul.Application.Harvests.Services;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Harvests;

public sealed class HarvestBatchEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid HarvestBatchId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly DateOnly HarvestDate =
        new(2027, 5, 1);

    [Fact]
    public async Task Create_ShouldReturnCreatedAndLocation()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

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
                    HarvestBatchResponse>();

        Assert.NotNull(body);
        Assert.Equal(HarvestBatchId, body!.Id);

        Assert.Equal(
            $"{BasePath}/{HarvestBatchId}",
            response.Headers.Location?.AbsolutePath);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(
            CropCycleId,
            service.LastCropCycleId);

        Assert.Equal(
            request,
            service.LastCreateRequest);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<HarvestBatchResponse>.Failure(
                    HarvestBatchErrors.Validation(
                        "Invalid harvest batch."))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<HarvestBatchResponse>.Failure(
                    HarvestBatchErrors.CodeAlreadyExists(
                        "HRV-001"))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

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

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var path =
            BasePath +
            "?status=Confirmed" +
            "&harvestDateFrom=2027-05-01" +
            "&harvestDateTo=2027-05-31" +
            "&quantityUnit=Kilogram" +
            "&qualityGrade=Grade%20A";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    List<HarvestBatchResponse>>();

        Assert.NotNull(body);
        Assert.Single(body!);
        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            HarvestBatchStatus.Confirmed,
            service.LastFilter!.Status);

        Assert.Equal(
            new DateOnly(2027, 5, 1),
            service.LastFilter.HarvestDateFrom);

        Assert.Equal(
            new DateOnly(2027, 5, 31),
            service.LastFilter.HarvestDateTo);

        Assert.Equal(
            HarvestQuantityUnit.Kilogram,
            service.LastFilter.QuantityUnit);

        Assert.Equal(
            "Grade A",
            service.LastFilter.QualityGrade);
    }

    [Fact]
    public async Task GetAll_WhenParentMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ListResult =
                Result<
                    IReadOnlyList<HarvestBatchResponse>>
                    .Failure(
                        HarvestBatchErrors
                            .CropCycleNotFound(
                                CropCycleId))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{HarvestBatchId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            HarvestBatchId,
            service.LastHarvestBatchId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<HarvestBatchResponse>.Failure(
                    HarvestBatchErrors.NotFound(
                        CropCycleId,
                        HarvestBatchId))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{HarvestBatchId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request =
            new UpdateHarvestBatchRequest(
                HarvestDate.AddDays(1),
                1200,
                100,
                HarvestQuantityUnit.Kilogram,
                "Grade B",
                "Gudang Timur",
                "Panen lanjutan");

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{HarvestBatchId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastUpdateRequest);

        Assert.Equal(
            HarvestBatchId,
            service.LastHarvestBatchId);
    }

    [Fact]
    public async Task UpdateDraft_WhenImmutable_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<HarvestBatchResponse>.Failure(
                    HarvestBatchErrors
                        .InvalidStatusTransition(
                            "Only draft harvest can change."))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{HarvestBatchId}",
            UpdateRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Confirm_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{BasePath}/{HarvestBatchId}/confirm");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            HarvestBatchId,
            service.LastHarvestBatchId);

        Assert.Equal(1, service.ConfirmCallCount);
    }

    [Fact]
    public async Task Confirm_WhenCycleNotInProgress_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<HarvestBatchResponse>.Failure(
                    HarvestBatchErrors
                        .CropCycleNotInProgress(
                            CropCycleId))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{BasePath}/{HarvestBatchId}/confirm");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request =
            new CancelHarvestBatchRequest(
                "Data panen salah.");

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{HarvestBatchId}/cancel",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastCancelRequest);

        Assert.Equal(
            HarvestBatchId,
            service.LastHarvestBatchId);
    }

    [Fact]
    public async Task Cancel_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<HarvestBatchResponse>.Failure(
                    HarvestBatchErrors.Validation(
                        "Cancellation reason required."))
        };

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{HarvestBatchId}/cancel",
            new CancelHarvestBatchRequest(""));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task NestedRoute_ShouldPassAllIdentifiers()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{HarvestBatchId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(
            CropCycleId,
            service.LastCropCycleId);

        Assert.Equal(
            HarvestBatchId,
            service.LastHarvestBatchId);
    }

    [Fact]
    public async Task InvalidHarvestBatchGuid_ShouldNotReachService()
    {
        var service = new StubService();

        using var factory =
            new HarvestBatchApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/not-a-guid");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            Guid.Empty,
            service.LastHarvestBatchId);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}" +
        $"/crop-cycles/{CropCycleId}/harvest-batches";

    private static CreateHarvestBatchRequest
        CreateRequest()
    {
        return new CreateHarvestBatchRequest(
            "HRV-001",
            HarvestDate,
            1000,
            25,
            HarvestQuantityUnit.Kilogram,
            "Grade A",
            "Gudang Utama",
            "Panen pagi");
    }

    private static UpdateHarvestBatchRequest
        UpdateRequest()
    {
        return new UpdateHarvestBatchRequest(
            HarvestDate,
            1000,
            25,
            HarvestQuantityUnit.Kilogram,
            "Grade A",
            "Gudang Utama",
            null);
    }

    private static HarvestBatchResponse
        CreateResponse(
            HarvestBatchStatus status =
                HarvestBatchStatus.Confirmed)
    {
        DateTime? confirmedAt =
            status == HarvestBatchStatus.Confirmed
                ? new DateTime(
                    2027,
                    5,
                    1,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)
                : null;

        var available =
            status == HarvestBatchStatus.Confirmed
                ? 975m
                : 0m;

        return new HarvestBatchResponse(
            HarvestBatchId,
            OrganizationId,
            CropCycleId,
            "HRV-001",
            HarvestDate,
            1000,
            25,
            975,
            HarvestQuantityUnit.Kilogram,
            "Grade A",
            "Gudang Utama",
            "Panen pagi",
            status,
            confirmedAt,
            null,
            0,
            available,
            new DateTime(
                2027,
                5,
                1,
                7,
                0,
                0,
                DateTimeKind.Utc),
            null);
    }

    private sealed class StubService :
        IHarvestBatchService
    {
        public Result<HarvestBatchResponse>
            SingleResult
        { get; set; } =
                Result<HarvestBatchResponse>.Success(
                    CreateResponse());

        public Result<
            IReadOnlyList<HarvestBatchResponse>>
            ListResult
        { get; set; } =
                Result<
                    IReadOnlyList<HarvestBatchResponse>>
                    .Success(
                        new[]
                        {
                            CreateResponse()
                        });

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

        public Guid LastHarvestBatchId
        {
            get;
            private set;
        }

        public HarvestBatchFilter? LastFilter
        {
            get;
            private set;
        }

        public CreateHarvestBatchRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateHarvestBatchRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public CancelHarvestBatchRequest?
            LastCancelRequest
        {
            get;
            private set;
        }

        public int ConfirmCallCount
        {
            get;
            private set;
        }

        public Task<Result<HarvestBatchResponse>>
            CreateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CreateHarvestBatchRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId);

            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<
            IReadOnlyList<HarvestBatchResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                HarvestBatchFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId);

            LastFilter = filter;

            return Task.FromResult(ListResult);
        }

        public Task<Result<HarvestBatchResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                harvestBatchId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<HarvestBatchResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid harvestBatchId,
                UpdateHarvestBatchRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                harvestBatchId);

            LastUpdateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<HarvestBatchResponse>>
            ConfirmAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                harvestBatchId);

            ConfirmCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<HarvestBatchResponse>>
            CancelAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid harvestBatchId,
                CancelHarvestBatchRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                harvestBatchId);

            LastCancelRequest = request;

            return Task.FromResult(SingleResult);
        }

        private void Capture(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId = default)
        {
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            LastHarvestBatchId = harvestBatchId;
        }
    }

    private sealed class HarvestBatchApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly IHarvestBatchService
            _service;

        public HarvestBatchApiFactory(
            IHarvestBatchService service)
        {
            _service = service;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IHarvestBatchService>();

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
}
