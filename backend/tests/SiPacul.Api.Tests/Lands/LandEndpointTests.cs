using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Lands;
using SiPacul.Application.Lands.Contracts;
using SiPacul.Application.Lands.Services;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Lands;

public sealed class LandEndpointTests
{
    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId);

        factory.Service.CreateResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/lands",
            CreateLandRequest());

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.Equal(land.Id, content!.Id);
        Assert.Equal(
            organizationId,
            content.OrganizationId);

        Assert.NotNull(response.Headers.Location);

        Assert.EndsWith(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        using var factory = new LandApiFactory();

        factory.Service.CreateResult =
            Result<LandResponse>.Failure(
                LandErrors.Validation(
                    "Land code is invalid."));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/lands",
            CreateLandRequest(code: " "));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            LandErrors.ValidationCode,
            body);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldReturnConflict()
    {
        using var factory = new LandApiFactory();

        factory.Service.CreateResult =
            Result<LandResponse>.Failure(
                LandErrors.CodeAlreadyExists(
                    "LHN-001"));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/lands",
            CreateLandRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            LandErrors.CodeAlreadyExistsCode,
            body);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<LandResponse>.Failure(
                LandErrors.OrganizationNotFound(
                    organizationId));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/lands",
            CreateLandRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnLands()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId);

        factory.Service.GetAllResult =
            Result<IReadOnlyList<LandResponse>>
                .Success(new[] { land });

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/lands");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(land.Id, content![0].Id);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnLand()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            includePlot: true);

        factory.Service.GetByIdResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.Equal(land.Id, content!.Id);
        Assert.Single(content.Plots);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();
        var landId = Guid.NewGuid();

        factory.Service.GetByIdResult =
            Result<LandResponse>.Failure(
                LandErrors.NotFound(
                    organizationId,
                    landId));

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{landId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            name: "Lahan Produksi");

        factory.Service.UpdateResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}",
            UpdateLandRequest(
                name: "Lahan Produksi"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            "Lahan Produksi",
            content!.Name);
    }

    [Fact]
    public async Task Update_WhenAreaCapacityExceeded_ShouldReturnConflict()
    {
        using var factory = new LandApiFactory();

        factory.Service.UpdateResult =
            Result<LandResponse>.Failure(
                LandErrors.AreaCapacityExceeded(
                    "Plot area exceeds land area."));

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"lands/{Guid.NewGuid()}",
            UpdateLandRequest(totalArea: 0.1m));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            LandErrors.AreaCapacityExceededCode,
            body);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnActiveLand()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            isActive: true);

        factory.Service.ActivateResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/activate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.True(content!.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ShouldReturnInactiveLand()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            isActive: false);

        factory.Service.DeactivateResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/deactivate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.False(content!.IsActive);
    }

    [Fact]
    public async Task AddPlot_WhenSuccessful_ShouldReturnUpdatedLand()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            includePlot: true);

        factory.Service.AddPlotResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/plots",
            AddLandPlotRequest());

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.Single(content!.Plots);
        Assert.Equal(
            "PTK-01",
            content.Plots[0].Code);
    }

    [Fact]
    public async Task AddPlot_WhenCodeExists_ShouldReturnConflict()
    {
        using var factory = new LandApiFactory();

        var landId = Guid.NewGuid();

        factory.Service.AddPlotResult =
            Result<LandResponse>.Failure(
                LandErrors.PlotCodeAlreadyExists(
                    landId,
                    "PTK-01"));

        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"lands/{landId}/plots",
            AddLandPlotRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            LandErrors.PlotCodeAlreadyExistsCode,
            body);
    }

    [Fact]
    public async Task UpdatePlot_WhenSuccessful_ShouldReturnUpdatedLand()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            includePlot: true,
            plotName: "Petak Timur");

        var plotId = land.Plots[0].Id;

        factory.Service.UpdatePlotResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/plots/{plotId}",
            UpdateLandPlotRequest(
                name: "Petak Timur"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            "Petak Timur",
            content!.Plots[0].Name);
    }

    [Fact]
    public async Task UpdatePlot_WhenMissing_ShouldReturnNotFound()
    {
        using var factory = new LandApiFactory();

        var landId = Guid.NewGuid();
        var plotId = Guid.NewGuid();

        factory.Service.UpdatePlotResult =
            Result<LandResponse>.Failure(
                LandErrors.PlotNotFound(
                    landId,
                    plotId));

        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/" +
            $"lands/{landId}/plots/{plotId}",
            UpdateLandPlotRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task RemovePlot_WhenSuccessful_ShouldReturnUpdatedLand()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId);

        factory.Service.RemovePlotResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/plots/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.Empty(content!.Plots);
    }

    [Fact]
    public async Task ActivatePlot_WhenSuccessful_ShouldReturnActivePlot()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            includePlot: true,
            plotIsActive: true);

        var plotId = land.Plots[0].Id;

        factory.Service.ActivatePlotResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/plots/{plotId}/activate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.True(content!.Plots[0].IsActive);
    }

    [Fact]
    public async Task DeactivatePlot_WhenSuccessful_ShouldReturnInactivePlot()
    {
        using var factory = new LandApiFactory();

        var organizationId = Guid.NewGuid();

        var land = CreateResponse(
            organizationId,
            includePlot: true,
            plotIsActive: false);

        var plotId = land.Plots[0].Id;

        factory.Service.DeactivatePlotResult =
            Result<LandResponse>.Success(land);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"lands/{land.Id}/plots/{plotId}/deactivate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                LandResponse>();

        Assert.NotNull(content);
        Assert.False(content!.Plots[0].IsActive);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = new LandApiFactory();
        var organizationId = Guid.NewGuid();
        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/organizations/{organizationId}/lands");

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
        using var factory = new LandApiFactory();
        factory.Authorization.Granted = false;
        var organizationId = Guid.NewGuid();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/lands");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            organizationId,
            factory.Authorization.LastOrganizationId);

        Assert.Equal(
            Permissions.LandsRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WithoutWritePermission_ShouldReturnForbidden()
    {
        using var factory = new LandApiFactory();
        factory.Authorization.Granted = false;
        var organizationId = Guid.NewGuid();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/lands",
            CreateLandRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.LandsWrite,
            factory.Authorization.LastPermission);
    }

    private static CreateLandRequest CreateLandRequest(
        string code = "LHN-001")
    {
        return new CreateLandRequest(
            code,
            "Lahan Utama",
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            "Desa Sukamaju",
            "Dekat saluran irigasi",
            -7.1m,
            110.1m,
            null);
    }

    private static UpdateLandRequest UpdateLandRequest(
        string name = "Lahan Utama",
        decimal totalArea = 1)
    {
        return new UpdateLandRequest(
            name,
            LandTenureType.Owned,
            totalArea,
            AreaUnit.Hectare,
            "Desa Sukamaju",
            "Dekat saluran irigasi",
            -7.1m,
            110.1m,
            null);
    }

    private static AddLandPlotRequest AddLandPlotRequest()
    {
        return new AddLandPlotRequest(
            "PTK-01",
            "Petak Satu",
            4_000,
            AreaUnit.SquareMeter,
            "Tanah gembur",
            null);
    }

    private static UpdateLandPlotRequest UpdateLandPlotRequest(
        string name = "Petak Satu")
    {
        return new UpdateLandPlotRequest(
            name,
            4_000,
            AreaUnit.SquareMeter,
            "Tanah gembur",
            null);
    }

    private static LandResponse CreateResponse(
        Guid organizationId,
        string name = "Lahan Utama",
        bool isActive = true,
        bool includePlot = false,
        string plotName = "Petak Satu",
        bool plotIsActive = true)
    {
        var landId = Guid.NewGuid();

        IReadOnlyList<LandPlotResponse> plots =
            includePlot
                ? new[]
                {
                    new LandPlotResponse(
                        Guid.NewGuid(),
                        landId,
                        "PTK-01",
                        plotName,
                        4_000,
                        AreaUnit.SquareMeter,
                        "Tanah gembur",
                        null,
                        plotIsActive,
                        DateTime.UtcNow,
                        null)
                }
                : Array.Empty<LandPlotResponse>();

        return new LandResponse(
            landId,
            organizationId,
            "LHN-001",
            name,
            LandTenureType.Owned,
            1,
            AreaUnit.Hectare,
            10_000,
            includePlot ? 4_000 : 0,
            "Desa Sukamaju",
            "Dekat saluran irigasi",
            -7.1m,
            110.1m,
            null,
            isActive,
            DateTime.UtcNow,
            null,
            plots);
    }

    private sealed class LandApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeLandService Service { get; } =
            new();

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

                services.RemoveAll<ILandService>();

                services.AddSingleton<ILandService>(
                    Service);
            });
        }
    }

    private sealed class FakeLandService :
        ILandService
    {
        public Result<LandResponse> CreateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<IReadOnlyList<LandResponse>>
            GetAllResult
        {
            get;
            set;
        } = Result<IReadOnlyList<LandResponse>>
            .Success(Array.Empty<LandResponse>());

        public Result<LandResponse> GetByIdResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<LandResponse> UpdateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<LandResponse> ActivateResult
        {
            get;
            set;
        } = SuccessResponse(isActive: true);

        public Result<LandResponse> DeactivateResult
        {
            get;
            set;
        } = SuccessResponse(isActive: false);

        public Result<LandResponse> AddPlotResult
        {
            get;
            set;
        } = SuccessResponse(includePlot: true);

        public Result<LandResponse> UpdatePlotResult
        {
            get;
            set;
        } = SuccessResponse(includePlot: true);

        public Result<LandResponse> RemovePlotResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<LandResponse> ActivatePlotResult
        {
            get;
            set;
        } = SuccessResponse(
            includePlot: true,
            plotIsActive: true);

        public Result<LandResponse> DeactivatePlotResult
        {
            get;
            set;
        } = SuccessResponse(
            includePlot: true,
            plotIsActive: false);

        public Task<Result<LandResponse>> CreateAsync(
            Guid organizationId,
            CreateLandRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<Result<IReadOnlyList<LandResponse>>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAllResult);
        }

        public Task<Result<LandResponse>> GetByIdAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<LandResponse>> UpdateAsync(
            Guid organizationId,
            Guid landId,
            UpdateLandRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateResult);
        }

        public Task<Result<LandResponse>> ActivateAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivateResult);
        }

        public Task<Result<LandResponse>> DeactivateAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeactivateResult);
        }

        public Task<Result<LandResponse>> AddPlotAsync(
            Guid organizationId,
            Guid landId,
            AddLandPlotRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AddPlotResult);
        }

        public Task<Result<LandResponse>> UpdatePlotAsync(
            Guid organizationId,
            Guid landId,
            Guid plotId,
            UpdateLandPlotRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdatePlotResult);
        }

        public Task<Result<LandResponse>> RemovePlotAsync(
            Guid organizationId,
            Guid landId,
            Guid plotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RemovePlotResult);
        }

        public Task<Result<LandResponse>> ActivatePlotAsync(
            Guid organizationId,
            Guid landId,
            Guid plotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivatePlotResult);
        }

        public Task<Result<LandResponse>> DeactivatePlotAsync(
            Guid organizationId,
            Guid landId,
            Guid plotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeactivatePlotResult);
        }

        private static Result<LandResponse> SuccessResponse(
            bool isActive = true,
            bool includePlot = false,
            bool plotIsActive = true)
        {
            return Result<LandResponse>.Success(
                CreateResponse(
                    Guid.NewGuid(),
                    isActive: isActive,
                    includePlot: includePlot,
                    plotIsActive: plotIsActive));
        }
    }
}
