using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Cultivation.CropCycles;
using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Application.Cultivation.CropCycles.Services;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Cultivation.CropCycles;

public sealed class CropCycleEndpointTests
{
    private static readonly DateOnly PlannedStart =
        new(2027, 1, 10);

    private static readonly DateOnly ExpectedHarvest =
        new(2027, 5, 10);

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId);

        factory.Service.CreateResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "crop-cycles",
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(cropCycle.Id, content!.Id);
        Assert.Equal(
            organizationId,
            content.OrganizationId);

        Assert.NotNull(response.Headers.Location);

        Assert.EndsWith(
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        using var factory = new CropCycleApiFactory();

        factory.Service.CreateResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    "Crop cycle code is invalid."));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            "crop-cycles",
            CreateRequest(code: " "));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CropCycleErrors.ValidationCode,
            body);
    }

    [Fact]
    public async Task Create_WhenReferenceMissing_ShouldReturnNotFound()
    {
        using var factory = new CropCycleApiFactory();

        var commodityId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors.CommodityNotFound(
                    commodityId));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            "crop-cycles",
            CreateRequest(
                commodityId: commodityId));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CropCycleErrors.CommodityNotFoundCode,
            body);
    }

    [Fact]
    public async Task Create_WhenScheduleConflicts_ShouldReturnConflict()
    {
        using var factory = new CropCycleApiFactory();

        var plotId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors.ScheduleConflict(
                    plotId));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            "crop-cycles",
            CreateRequest(
                landPlotId: plotId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CropCycleErrors.ScheduleConflictCode,
            body);
    }

    [Fact]
    public async Task GetAll_ShouldReturnFilteredCropCycles()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();
        var landId = Guid.NewGuid();
        var plotId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId,
            commodityId: commodityId,
            landId: landId,
            landPlotId: plotId,
            status: CropCycleStatus.InProgress);

        factory.Service.GetAllResult =
            Result<IReadOnlyList<CropCycleResponse>>
                .Success(new[] { cropCycle });

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "crop-cycles" +
            "?status=InProgress" +
            $"&commodityId={commodityId}" +
            $"&landId={landId}" +
            $"&landPlotId={plotId}" +
            "&plannedStartFrom=2027-01-01" +
            "&plannedStartTo=2027-12-31");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(cropCycle.Id, content![0].Id);

        Assert.NotNull(factory.Service.LastFilter);
        Assert.Equal(
            CropCycleStatus.InProgress,
            factory.Service.LastFilter!.Status);
        Assert.Equal(
            commodityId,
            factory.Service.LastFilter.CommodityId);
        Assert.Equal(
            landId,
            factory.Service.LastFilter.LandId);
        Assert.Equal(
            plotId,
            factory.Service.LastFilter.LandPlotId);
        Assert.Equal(
            new DateOnly(2027, 1, 1),
            factory.Service.LastFilter.PlannedStartFrom);
        Assert.Equal(
            new DateOnly(2027, 12, 31),
            factory.Service.LastFilter.PlannedStartTo);
    }

    [Fact]
    public async Task GetAll_WhenFilterInvalid_ShouldReturnBadRequest()
    {
        using var factory = new CropCycleApiFactory();

        factory.Service.GetAllResult =
            Result<IReadOnlyList<CropCycleResponse>>
                .Failure(
                    CropCycleErrors.Validation(
                        "Date filter is invalid."));

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            "crop-cycles" +
            "?plannedStartFrom=2027-12-31" +
            "&plannedStartTo=2027-01-01");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnCropCycle()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();
        var cropCycle = CreateResponse(
            organizationId);

        factory.Service.GetByIdResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(cropCycle.Id, content!.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();
        var cropCycleId = Guid.NewGuid();

        factory.Service.GetByIdResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors.NotFound(
                    organizationId,
                    cropCycleId));

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycleId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlan_WhenSuccessful_ShouldReturnOk()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId,
            name: "Musim Tanam Organik");

        factory.Service.UpdatePlanResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}",
            UpdatePlanRequest(
                name: "Musim Tanam Organik"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            "Musim Tanam Organik",
            content!.Name);
    }

    [Fact]
    public async Task UpdatePlan_WhenScheduleConflicts_ShouldReturnConflict()
    {
        using var factory = new CropCycleApiFactory();

        var plotId = Guid.NewGuid();

        factory.Service.UpdatePlanResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors.ScheduleConflict(
                    plotId));

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"crop-cycles/{Guid.NewGuid()}",
            UpdatePlanRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Start_WhenSuccessful_ShouldReturnInProgress()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId,
            status: CropCycleStatus.InProgress,
            actualStartDate: PlannedStart);

        factory.Service.StartResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}/start",
            new StartCropCycleRequest(
                PlannedStart));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            CropCycleStatus.InProgress,
            content!.Status);
        Assert.Equal(
            PlannedStart,
            content.ActualStartDate);
    }

    [Fact]
    public async Task Start_WhenAnotherCycleActive_ShouldReturnConflict()
    {
        using var factory = new CropCycleApiFactory();

        factory.Service.StartResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors
                    .ActiveCycleAlreadyExists(
                        Guid.NewGuid()));

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"crop-cycles/{Guid.NewGuid()}/start",
            new StartCropCycleRequest(
                PlannedStart));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Complete_WhenSuccessful_ShouldReturnCompleted()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId,
            status: CropCycleStatus.Completed,
            actualStartDate: PlannedStart,
            actualHarvestDate: ExpectedHarvest);

        factory.Service.CompleteResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}/complete",
            new CompleteCropCycleRequest(
                ExpectedHarvest));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            CropCycleStatus.Completed,
            content!.Status);
        Assert.Equal(
            ExpectedHarvest,
            content.ActualHarvestDate);
    }

    [Fact]
    public async Task Complete_WhenTransitionInvalid_ShouldReturnConflict()
    {
        using var factory = new CropCycleApiFactory();

        factory.Service.CompleteResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors
                    .InvalidStatusTransition(
                        "Invalid transition."));

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"crop-cycles/{Guid.NewGuid()}/complete",
            new CompleteCropCycleRequest(
                ExpectedHarvest));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WhenSuccessful_ShouldReturnCancelled()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId,
            status: CropCycleStatus.Cancelled,
            cancellationReason: "Perubahan rencana");

        factory.Service.CancelResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}/cancel",
            new CancelCropCycleRequest(
                "Perubahan rencana"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            CropCycleStatus.Cancelled,
            content!.Status);
        Assert.Equal(
            "Perubahan rencana",
            content.CancellationReason);
    }

    [Fact]
    public async Task Cancel_WhenReasonInvalid_ShouldReturnBadRequest()
    {
        using var factory = new CropCycleApiFactory();

        factory.Service.CancelResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    "Cancellation reason is required."));

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"crop-cycles/{Guid.NewGuid()}/cancel",
            new CancelCropCycleRequest(" "));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateNotes_WhenSuccessful_ShouldReturnUpdatedNotes()
    {
        using var factory = new CropCycleApiFactory();

        var organizationId = Guid.NewGuid();

        var cropCycle = CreateResponse(
            organizationId,
            notes: "Catatan lapangan");

        factory.Service.UpdateNotesResult =
            Result<CropCycleResponse>.Success(
                cropCycle);

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{organizationId}/" +
            $"crop-cycles/{cropCycle.Id}/notes",
            new UpdateCropCycleNotesRequest(
                "Catatan lapangan"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CropCycleResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            "Catatan lapangan",
            content!.Notes);
    }

    [Fact]
    public async Task UpdateNotes_WhenTerminal_ShouldReturnConflict()
    {
        using var factory = new CropCycleApiFactory();

        factory.Service.UpdateNotesResult =
            Result<CropCycleResponse>.Failure(
                CropCycleErrors
                    .InvalidStatusTransition(
                        "Terminal cycle cannot be edited."));

        using var client = factory.CreateHttpsClient();

        var response = await SendPatchAsync(
            client,
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"crop-cycles/{Guid.NewGuid()}/notes",
            new UpdateCropCycleNotesRequest(
                "Tidak boleh"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    private static async Task<HttpResponseMessage>
        SendPatchAsync<TRequest>(
            HttpClient client,
            string path,
            TRequest request)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Patch,
            path)
        {
            Content = JsonContent.Create(request)
        };

        return await client.SendAsync(message);
    }

    private static CreateCropCycleRequest CreateRequest(
        string code = "SC-PADI-001",
        Guid? commodityId = null,
        Guid? landPlotId = null)
    {
        return new CreateCropCycleRequest(
            code,
            "Musim Tanam Padi",
            commodityId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            landPlotId ?? Guid.NewGuid(),
            5_000,
            AreaUnit.SquareMeter,
            PlannedStart,
            ExpectedHarvest,
            null);
    }

    private static UpdateCropCyclePlanRequest
        UpdatePlanRequest(
            string name = "Musim Tanam Padi")
    {
        return new UpdateCropCyclePlanRequest(
            name,
            Guid.NewGuid(),
            5_000,
            AreaUnit.SquareMeter,
            PlannedStart,
            ExpectedHarvest,
            null);
    }

    private static CropCycleResponse CreateResponse(
        Guid organizationId,
        string name = "Musim Tanam Padi",
        Guid? commodityId = null,
        Guid? landId = null,
        Guid? landPlotId = null,
        CropCycleStatus status =
            CropCycleStatus.Planned,
        DateOnly? actualStartDate = null,
        DateOnly? actualHarvestDate = null,
        string? cancellationReason = null,
        string? notes = null)
    {
        return new CropCycleResponse(
            Guid.NewGuid(),
            organizationId,
            "SC-PADI-001",
            name,
            commodityId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            landId ?? Guid.NewGuid(),
            landPlotId ?? Guid.NewGuid(),
            5_000,
            AreaUnit.SquareMeter,
            5_000,
            PlannedStart,
            ExpectedHarvest,
            actualStartDate,
            actualHarvestDate,
            status,
            cancellationReason,
            notes,
            DateTime.UtcNow,
            null);
    }

    private sealed class CropCycleApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeCropCycleService Service { get; } =
            new();

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("https://localhost")
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
                services.RemoveAll<ICropCycleService>();

                services.AddSingleton<ICropCycleService>(
                    Service);
            });
        }
    }

    private sealed class FakeCropCycleService :
        ICropCycleService
    {
        public Result<CropCycleResponse> CreateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<IReadOnlyList<CropCycleResponse>>
            GetAllResult
        {
            get;
            set;
        } = Result<IReadOnlyList<CropCycleResponse>>
            .Success(
                Array.Empty<CropCycleResponse>());

        public Result<CropCycleResponse> GetByIdResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CropCycleResponse> UpdatePlanResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CropCycleResponse> StartResult
        {
            get;
            set;
        } = SuccessResponse(
            CropCycleStatus.InProgress);

        public Result<CropCycleResponse> CompleteResult
        {
            get;
            set;
        } = SuccessResponse(
            CropCycleStatus.Completed);

        public Result<CropCycleResponse> CancelResult
        {
            get;
            set;
        } = SuccessResponse(
            CropCycleStatus.Cancelled);

        public Result<CropCycleResponse> UpdateNotesResult
        {
            get;
            set;
        } = SuccessResponse();

        public CropCycleFilter? LastFilter
        {
            get;
            private set;
        }

        public Task<Result<CropCycleResponse>> CreateAsync(
            Guid organizationId,
            CreateCropCycleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<
            Result<IReadOnlyList<CropCycleResponse>>>
            GetAllAsync(
                Guid organizationId,
                CropCycleFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            LastFilter = filter;

            return Task.FromResult(GetAllResult);
        }

        public Task<Result<CropCycleResponse>> GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<CropCycleResponse>>
            UpdatePlanAsync(
                Guid organizationId,
                Guid cropCycleId,
                UpdateCropCyclePlanRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                UpdatePlanResult);
        }

        public Task<Result<CropCycleResponse>> StartAsync(
            Guid organizationId,
            Guid cropCycleId,
            StartCropCycleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StartResult);
        }

        public Task<Result<CropCycleResponse>> CompleteAsync(
            Guid organizationId,
            Guid cropCycleId,
            CompleteCropCycleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CompleteResult);
        }

        public Task<Result<CropCycleResponse>> CancelAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancelCropCycleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CancelResult);
        }

        public Task<Result<CropCycleResponse>>
            UpdateNotesAsync(
                Guid organizationId,
                Guid cropCycleId,
                UpdateCropCycleNotesRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                UpdateNotesResult);
        }

        private static Result<CropCycleResponse>
            SuccessResponse(
                CropCycleStatus status =
                    CropCycleStatus.Planned)
        {
            return Result<CropCycleResponse>.Success(
                CreateResponse(
                    Guid.NewGuid(),
                    status: status));
        }
    }
}
