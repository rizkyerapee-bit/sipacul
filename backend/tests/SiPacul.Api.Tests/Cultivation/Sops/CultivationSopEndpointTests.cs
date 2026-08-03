using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Cultivation.Sops;
using SiPacul.Application.Cultivation.Sops.Contracts;
using SiPacul.Application.Cultivation.Sops.Services;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Cultivation.Sops;

public sealed class CultivationSopEndpointTests
{
    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            commodityId);

        factory.Service.CreateResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "cultivation-sops",
            new CreateCultivationSopRequest(
                commodityId,
                "SOP Budidaya Padi",
                "Panduan standar"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse>();

        Assert.NotNull(content);
        Assert.Equal(cultivationSop.Id, content!.Id);
        Assert.NotNull(response.Headers.Location);

        Assert.EndsWith(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        using var factory = new CultivationSopApiFactory();

        factory.Service.CreateResult =
            Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    "Cultivation SOP name cannot be empty."));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            "cultivation-sops",
            new CreateCultivationSopRequest(
                Guid.NewGuid(),
                " ",
                null));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenCommodityMissing_ShouldReturnNotFound()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.CommodityNotFound(
                    organizationId,
                    commodityId));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "cultivation-sops",
            new CreateCultivationSopRequest(
                commodityId,
                "SOP Padi",
                null));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenNameExists_ShouldReturnConflict()
    {
        using var factory = new CultivationSopApiFactory();

        var commodityId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.NameAlreadyExists(
                    commodityId,
                    "SOP Padi"));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            "cultivation-sops",
            new CreateCultivationSopRequest(
                commodityId,
                "SOP Padi",
                null));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnSopsAndBindCommodityFilter()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            commodityId);

        factory.Service.GetAllResult =
            Result<IReadOnlyList<CultivationSopResponse>>
                .Success(
                    new[]
                    {
                        cultivationSop
                    });

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops?commodityId={commodityId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(
            commodityId,
            factory.Service.LastCommodityId);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();
        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid());

        factory.Service.GetByIdResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse>();

        Assert.NotNull(content);
        Assert.Equal(cultivationSop.Id, content!.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();
        var cultivationSopId = Guid.NewGuid();

        factory.Service.GetByIdResult =
            Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.NotFound(
                    organizationId,
                    cultivationSopId));

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSopId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid(),
            name: "SOP Padi Organik");

        factory.Service.UpdateResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}",
            new UpdateCultivationSopRequest(
                "SOP Padi Organik",
                "Panduan organik"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            "SOP Padi Organik",
            content!.Name);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnActiveSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid(),
            isActive: true);

        factory.Service.ActivateResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}/activate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse>();

        Assert.NotNull(content);
        Assert.True(content!.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ShouldReturnInactiveSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid(),
            isActive: false);

        factory.Service.DeactivateResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}/deactivate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse>();

        Assert.NotNull(content);
        Assert.False(content!.IsActive);
    }

    [Fact]
    public async Task AddStep_WhenSuccessful_ShouldReturnUpdatedSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid(),
            includeStep: true);

        factory.Service.AddStepResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}/steps",
            new AddCultivationSopStepRequest(
                "Persiapan Lahan",
                null,
                -14,
                7,
                true));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CultivationSopResponse>();

        Assert.NotNull(content);
        Assert.Single(content!.Steps);
    }

    [Fact]
    public async Task UpdateStep_WhenSuccessful_ShouldReturnUpdatedSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid(),
            includeStep: true);

        var stepId = cultivationSop.Steps[0].Id;

        factory.Service.UpdateStepResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}/" +
            $"steps/{stepId}",
            new UpdateCultivationSopStepRequest(
                "Persiapan Lahan",
                "Bersihkan lahan",
                -14,
                7,
                true));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task RemoveStep_WhenSuccessful_ShouldReturnUpdatedSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid());

        factory.Service.RemoveStepResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}/" +
            $"steps/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task MoveStep_WhenSuccessful_ShouldReturnUpdatedSop()
    {
        using var factory = new CultivationSopApiFactory();

        var organizationId = Guid.NewGuid();

        var cultivationSop = CreateResponse(
            organizationId,
            Guid.NewGuid(),
            includeStep: true);

        var stepId = cultivationSop.Steps[0].Id;

        factory.Service.MoveStepResult =
            Result<CultivationSopResponse>.Success(
                cultivationSop);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"cultivation-sops/{cultivationSop.Id}/" +
            $"steps/{stepId}/move")
        {
            Content = JsonContent.Create(
                new MoveCultivationSopStepRequest(1))
        };

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = new CultivationSopApiFactory();
        var organizationId = Guid.NewGuid();
        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/organizations/{organizationId}/" +
            "cultivation-sops");

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
        using var factory = new CultivationSopApiFactory();
        factory.Authorization.Granted = false;
        var organizationId = Guid.NewGuid();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "cultivation-sops");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            organizationId,
            factory.Authorization.LastOrganizationId);

        Assert.Equal(
            Permissions.MasterDataRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WithoutWritePermission_ShouldReturnForbidden()
    {
        using var factory = new CultivationSopApiFactory();
        factory.Authorization.Granted = false;
        var organizationId = Guid.NewGuid();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "cultivation-sops",
            new CreateCultivationSopRequest(
                Guid.NewGuid(),
                "SOP Padi",
                null));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.MasterDataWrite,
            factory.Authorization.LastPermission);
    }

    private static CultivationSopResponse CreateResponse(
        Guid organizationId,
        Guid commodityId,
        string name = "SOP Budidaya Padi",
        bool isActive = true,
        bool includeStep = false)
    {
        var cultivationSopId = Guid.NewGuid();

        IReadOnlyList<CultivationSopStepResponse> steps =
            includeStep
                ? new[]
                {
                    new CultivationSopStepResponse(
                        Guid.NewGuid(),
                        organizationId,
                        cultivationSopId,
                        1,
                        "Persiapan Lahan",
                        null,
                        -14,
                        7,
                        true,
                        DateTime.UtcNow,
                        null)
                }
                : Array.Empty<CultivationSopStepResponse>();

        return new CultivationSopResponse(
            cultivationSopId,
            organizationId,
            commodityId,
            name,
            null,
            isActive,
            DateTime.UtcNow,
            null,
            steps);
    }

    private sealed class CultivationSopApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeCultivationSopService Service
        {
            get;
        } = new();

        public ConfigurableOrganizationPermissionService
            Authorization
        { get; } = new();

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
                services.AddOrganizationAuthorizationForTests(
                    Authorization);

                services.RemoveAll<
                    ICultivationSopService>();

                services.AddSingleton<
                    ICultivationSopService>(
                    Service);
            });
        }
    }

    private sealed class FakeCultivationSopService :
        ICultivationSopService
    {
        public Guid? LastCommodityId { get; private set; }

        public Result<CultivationSopResponse> CreateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<
            IReadOnlyList<CultivationSopResponse>>
            GetAllResult
        {
            get;
            set;
        } = Result<
            IReadOnlyList<CultivationSopResponse>>
            .Success(
                Array.Empty<CultivationSopResponse>());

        public Result<CultivationSopResponse> GetByIdResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CultivationSopResponse> UpdateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CultivationSopResponse> ActivateResult
        {
            get;
            set;
        } = SuccessResponse(isActive: true);

        public Result<CultivationSopResponse> DeactivateResult
        {
            get;
            set;
        } = SuccessResponse(isActive: false);

        public Result<CultivationSopResponse> AddStepResult
        {
            get;
            set;
        } = SuccessResponse(includeStep: true);

        public Result<CultivationSopResponse> UpdateStepResult
        {
            get;
            set;
        } = SuccessResponse(includeStep: true);

        public Result<CultivationSopResponse> RemoveStepResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CultivationSopResponse> MoveStepResult
        {
            get;
            set;
        } = SuccessResponse(includeStep: true);

        public Task<Result<CultivationSopResponse>>
            CreateAsync(
                Guid organizationId,
                CreateCultivationSopRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<
            Result<IReadOnlyList<CultivationSopResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid? commodityId = null,
                CancellationToken cancellationToken = default)
        {
            LastCommodityId = commodityId;

            return Task.FromResult(GetAllResult);
        }

        public Task<Result<CultivationSopResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cultivationSopId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<CultivationSopResponse>>
            UpdateAsync(
                Guid organizationId,
                Guid cultivationSopId,
                UpdateCultivationSopRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateResult);
        }

        public Task<Result<CultivationSopResponse>>
            ActivateAsync(
                Guid organizationId,
                Guid cultivationSopId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivateResult);
        }

        public Task<Result<CultivationSopResponse>>
            DeactivateAsync(
                Guid organizationId,
                Guid cultivationSopId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeactivateResult);
        }

        public Task<Result<CultivationSopResponse>>
            AddStepAsync(
                Guid organizationId,
                Guid cultivationSopId,
                AddCultivationSopStepRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AddStepResult);
        }

        public Task<Result<CultivationSopResponse>>
            UpdateStepAsync(
                Guid organizationId,
                Guid cultivationSopId,
                Guid stepId,
                UpdateCultivationSopStepRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateStepResult);
        }

        public Task<Result<CultivationSopResponse>>
            RemoveStepAsync(
                Guid organizationId,
                Guid cultivationSopId,
                Guid stepId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RemoveStepResult);
        }

        public Task<Result<CultivationSopResponse>>
            MoveStepAsync(
                Guid organizationId,
                Guid cultivationSopId,
                Guid stepId,
                MoveCultivationSopStepRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MoveStepResult);
        }

        private static Result<CultivationSopResponse>
            SuccessResponse(
                bool isActive = true,
                bool includeStep = false)
        {
            return Result<CultivationSopResponse>.Success(
                CreateResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    isActive: isActive,
                    includeStep: includeStep));
        }
    }
}
