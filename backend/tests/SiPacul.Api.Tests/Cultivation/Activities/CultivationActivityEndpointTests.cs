using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Cultivation.Activities;
using SiPacul.Application.Cultivation.Activities.Contracts;
using SiPacul.Application.Cultivation.Activities.Services;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Cultivation.Activities;

public sealed class CultivationActivityEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid ActivityId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly Guid ResourceId =
        Guid.Parse(
            "40000000-0000-0000-0000-000000000001");

    private static readonly Guid SopStepId =
        Guid.Parse(
            "50000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Create_ShouldReturnCreatedAndLocation()
    {
        var service = new StubService();
        using var factory = new ActivityApiFactory(service);
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
                    CultivationActivityResponse>();

        Assert.NotNull(body);
        Assert.Equal(ActivityId, body!.Id);
        Assert.Equal(
            $"{BasePath}/{ActivityId}",
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
                Result<CultivationActivityResponse>
                    .Failure(
                        CultivationActivityErrors
                            .Validation(
                                "Invalid activity."))
        };

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

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
                Result<CultivationActivityResponse>
                    .Failure(
                        CultivationActivityErrors
                            .CodeAlreadyExists(
                                "ACT-001"))
        };

        using var factory = new ActivityApiFactory(service);
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

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var path =
            BasePath +
            "?status=InProgress" +
            "&activityType=Fertilization" +
            "&plannedFrom=2027-01-01" +
            "&plannedTo=2027-01-31" +
            $"&cultivationSopStepId={SopStepId}";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    List<CultivationActivityResponse>>();

        Assert.NotNull(body);
        Assert.Single(body!);

        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            CultivationActivityStatus.InProgress,
            service.LastFilter!.Status);

        Assert.Equal(
            CultivationActivityType.Fertilization,
            service.LastFilter.ActivityType);

        Assert.Equal(
            new DateOnly(2027, 1, 1),
            service.LastFilter.PlannedFrom);

        Assert.Equal(
            new DateOnly(2027, 1, 31),
            service.LastFilter.PlannedTo);

        Assert.Equal(
            SopStepId,
            service.LastFilter.CultivationSopStepId);
    }

    [Fact]
    public async Task GetAll_WhenParentMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ListResult =
                Result<
                    IReadOnlyList<
                        CultivationActivityResponse>>
                    .Failure(
                        CultivationActivityErrors
                            .CropCycleNotFound(
                                CropCycleId))
        };

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{ActivityId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            ActivityId,
            service.LastActivityId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CultivationActivityResponse>
                    .Failure(
                        CultivationActivityErrors
                            .NotFound(
                                CropCycleId,
                                ActivityId))
        };

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{ActivityId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlan_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new UpdateCultivationActivityPlanRequest(
                "Pemupukan Susulan",
                CultivationActivityType.Fertilization,
                new DateOnly(2027, 1, 20),
                "Gunakan dosis sesuai kondisi tanaman.");

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{ActivityId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastUpdatePlanRequest);
    }

    [Fact]
    public async Task Start_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new StartCultivationActivityRequest(
                new DateOnly(2027, 1, 5));

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{ActivityId}/start",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastStartRequest);
    }

    [Fact]
    public async Task Complete_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new CompleteCultivationActivityRequest(
                new DateOnly(2027, 1, 7),
                "Selesai",
                null,
                SopComplianceStatus.Compliant,
                null);

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{ActivityId}/complete",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastCompleteRequest);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new CancelCultivationActivityRequest(
                "Hujan lebat.");

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{ActivityId}/cancel",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastCancelRequest);
    }

    [Fact]
    public async Task UpdateNotes_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new UpdateCultivationActivityNotesRequest(
                "Catatan lapangan.",
                "Daun menguning.");

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{ActivityId}/notes",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastNotesRequest);
    }

    [Fact]
    public async Task AddResource_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new AddCultivationActivityResourceRequest(
                CultivationResourceType.Material,
                "Pupuk Urea",
                100,
                "kg",
                4500,
                null);

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/{ActivityId}/resources",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastAddResourceRequest);
    }

    [Fact]
    public async Task UpdateResource_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new UpdateCultivationActivityResourceRequest(
                "Pupuk Urea",
                120,
                "kg",
                4600,
                "Harga aktual.");

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{ActivityId}/resources/" +
            ResourceId,
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            ResourceId,
            service.LastResourceId);

        Assert.Equal(
            request,
            service.LastUpdateResourceRequest);
    }

    [Fact]
    public async Task RemoveResource_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync(
            $"{BasePath}/{ActivityId}/resources/" +
            ResourceId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            ResourceId,
            service.LastResourceId);
    }

    [Fact]
    public async Task RemoveResource_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CultivationActivityResponse>
                    .Failure(
                        CultivationActivityErrors
                            .ResourceNotFound(
                                ResourceId))
        };

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync(
            $"{BasePath}/{ActivityId}/resources/" +
            ResourceId);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Mutation_WhenCropCycleTerminal_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CultivationActivityResponse>
                    .Failure(
                        CultivationActivityErrors
                            .CropCycleTerminal(
                                CropCycleId))
        };

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{ActivityId}/start",
            new StartCultivationActivityRequest(
                new DateOnly(2027, 1, 5)));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task NestedRoute_ShouldPassAllIdentifiers()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync(
            $"{BasePath}/{ActivityId}/resources/" +
            ResourceId);

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
            ActivityId,
            service.LastActivityId);

        Assert.Equal(
            ResourceId,
            service.LastResourceId);
    }

    [Fact]
    public async Task InvalidActivityGuid_ShouldNotReachService()
    {
        var service = new StubService();

        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/not-a-guid");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            Guid.Empty,
            service.LastActivityId);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var service = new StubService();
        using var factory = new ActivityApiFactory(service);
        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BasePath);

        request.Headers.Add(
            OrganizationAuthorizationTestSupport
                .UnauthenticatedHeaderName,
            "true");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(0, factory.Authorization.CallCount);
    }

    [Fact]
    public async Task GetAll_WithoutReadPermission_ShouldReturnForbidden()
    {
        var service = new StubService();
        using var factory = new ActivityApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            OrganizationId,
            factory.Authorization.LastOrganizationId);

        Assert.Equal(
            Permissions.CultivationRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WithoutWritePermission_ShouldReturnForbidden()
    {
        var service = new StubService();
        using var factory = new ActivityApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.CultivationWrite,
            factory.Authorization.LastPermission);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}" +
        $"/crop-cycles/{CropCycleId}/activities";

    private static CreateCultivationActivityRequest
        CreateRequest()
    {
        return new CreateCultivationActivityRequest(
            "ACT-001",
            "Pengolahan Tanah",
            CultivationActivityType.LandPreparation,
            new DateOnly(2027, 1, 5),
            null,
            null,
            null);
    }

    private static CultivationActivityResponse
        CreateResponse()
    {
        return new CultivationActivityResponse(
            ActivityId,
            OrganizationId,
            CropCycleId,
            "ACT-001",
            "Pengolahan Tanah",
            CultivationActivityType.LandPreparation,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            new DateOnly(2027, 1, 5),
            null,
            null,
            CultivationActivityStatus.Planned,
            SopComplianceStatus.NotApplicable,
            null,
            null,
            null,
            null,
            null,
            0,
            Array.Empty<
                CultivationActivityResourceResponse>(),
            new DateTime(
                2027,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc),
            null);
    }

    private sealed class StubService :
        ICultivationActivityService
    {
        public Result<CultivationActivityResponse>
            SingleResult
        { get; set; } =
                Result<CultivationActivityResponse>
                    .Success(CreateResponse());

        public Result<
            IReadOnlyList<CultivationActivityResponse>>
            ListResult
        { get; set; } =
                Result<
                    IReadOnlyList<
                        CultivationActivityResponse>>
                    .Success(
                        new[]
                        {
                            CreateResponse()
                        });

        public Guid LastOrganizationId { get; private set; }

        public Guid LastCropCycleId { get; private set; }

        public Guid LastActivityId { get; private set; }

        public Guid LastResourceId { get; private set; }

        public CultivationActivityFilter?
            LastFilter
        { get; private set; }

        public CreateCultivationActivityRequest?
            LastCreateRequest
        { get; private set; }

        public UpdateCultivationActivityPlanRequest?
            LastUpdatePlanRequest
        { get; private set; }

        public StartCultivationActivityRequest?
            LastStartRequest
        { get; private set; }

        public CompleteCultivationActivityRequest?
            LastCompleteRequest
        { get; private set; }

        public CancelCultivationActivityRequest?
            LastCancelRequest
        { get; private set; }

        public UpdateCultivationActivityNotesRequest?
            LastNotesRequest
        { get; private set; }

        public AddCultivationActivityResourceRequest?
            LastAddResourceRequest
        { get; private set; }

        public UpdateCultivationActivityResourceRequest?
            LastUpdateResourceRequest
        { get; private set; }

        public Task<Result<CultivationActivityResponse>>
            CreateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CreateCultivationActivityRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId);

            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<
            IReadOnlyList<CultivationActivityResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                CultivationActivityFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId);

            LastFilter = filter;

            return Task.FromResult(ListResult);
        }

        public Task<Result<CultivationActivityResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            UpdatePlanAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                UpdateCultivationActivityPlanRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            LastUpdatePlanRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            StartAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                StartCultivationActivityRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            LastStartRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            CompleteAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                CompleteCultivationActivityRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            LastCompleteRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            CancelAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                CancelCultivationActivityRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            LastCancelRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            UpdateExecutionNotesAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                UpdateCultivationActivityNotesRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            LastNotesRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            AddResourceAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                AddCultivationActivityResourceRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId);

            LastAddResourceRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            UpdateResourceAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                Guid resourceId,
                UpdateCultivationActivityResourceRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId,
                resourceId);

            LastUpdateResourceRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationActivityResponse>>
            RemoveResourceAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid activityId,
                Guid resourceId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                activityId,
                resourceId);

            return Task.FromResult(SingleResult);
        }

        private void Capture(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId = default,
            Guid resourceId = default)
        {
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            LastActivityId = activityId;
            LastResourceId = resourceId;
        }
    }

    private sealed class ActivityApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly ICultivationActivityService
            _service;

        public ConfigurableOrganizationPermissionService
            Authorization
        { get; } = new();

        public ActivityApiFactory(
            ICultivationActivityService service)
        {
            _service = service;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddOrganizationAuthorizationForTests(
                    Authorization);

                services.RemoveAll<
                    ICultivationActivityService>();

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
