using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.MasterData.Commodities;
using SiPacul.Application.MasterData.Commodities.Contracts;
using SiPacul.Application.MasterData.Commodities.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.MasterData.Commodities;

public sealed class CommodityEndpointTests
{
    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var commodity = CreateCommodityResponse(
            organizationId,
            categoryId);

        factory.Service.CreateResult =
            Result<CommodityResponse>.Success(
                commodity);

        using var client =
            factory.CreateHttpsClient();

        var request = new CreateCommodityRequest(
            "PADI",
            "Padi",
            categoryId,
            "Oryza sativa",
            "Tanaman pangan");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityResponse>();

        Assert.NotNull(content);
        Assert.Equal(commodity.Id, content!.Id);

        Assert.Equal(
            organizationId,
            content.OrganizationId);

        Assert.NotNull(
            response.Headers.Location);

        Assert.EndsWith(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodities/{commodity.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CommodityResponse>.Failure(
                CommodityErrors.Validation(
                    "Commodity code is invalid."));

        using var client =
            factory.CreateHttpsClient();

        var request = new CreateCommodityRequest(
            " ",
            "Padi",
            Guid.NewGuid(),
            null,
            null);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CommodityErrors.ValidationCode,
            body);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldReturnConflict()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CommodityResponse>.Failure(
                CommodityErrors.CodeAlreadyExists(
                    "PADI"));

        using var client =
            factory.CreateHttpsClient();

        var request = new CreateCommodityRequest(
            "PADI",
            "Padi",
            Guid.NewGuid(),
            null,
            null);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CommodityErrors.CodeAlreadyExistsCode,
            body);
    }

    [Fact]
    public async Task Create_WhenCategoryMissing_ShouldReturnNotFound()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CommodityResponse>.Failure(
                CommodityErrors.CategoryNotFound(
                    organizationId,
                    categoryId));

        using var client =
            factory.CreateHttpsClient();

        var request = new CreateCommodityRequest(
            "PADI",
            "Padi",
            categoryId,
            null,
            null);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CommodityErrors.CategoryNotFoundCode,
            body);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCommodities()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        var commodity = CreateCommodityResponse(
            organizationId,
            Guid.NewGuid());

        factory.Service.GetAllResult =
            Result<IReadOnlyList<CommodityResponse>>
                .Success(
                    new[]
                    {
                        commodity
                    });

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(
            commodity.Id,
            content![0].Id);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnCommodity()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        var commodity = CreateCommodityResponse(
            organizationId,
            Guid.NewGuid());

        factory.Service.GetByIdResult =
            Result<CommodityResponse>.Success(
                commodity);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodities/{commodity.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            commodity.Id,
            content!.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();

        factory.Service.GetByIdResult =
            Result<CommodityResponse>.Failure(
                CommodityErrors.NotFound(
                    organizationId,
                    commodityId));

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodities/{commodityId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CommodityErrors.NotFoundCode,
            body);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var commodity = CreateCommodityResponse(
            organizationId,
            categoryId,
            name: "Padi Organik");

        factory.Service.UpdateResult =
            Result<CommodityResponse>.Success(
                commodity);

        using var client =
            factory.CreateHttpsClient();

        var request = new UpdateCommodityRequest(
            "Padi Organik",
            categoryId,
            "Oryza sativa",
            "Padi budidaya organik");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodities/{commodity.Id}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityResponse>();

        Assert.NotNull(content);

        Assert.Equal(
            "Padi Organik",
            content!.Name);

        Assert.Equal(
            categoryId,
            content.CommodityCategoryId);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnActiveCommodity()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        var commodity = CreateCommodityResponse(
            organizationId,
            Guid.NewGuid(),
            isActive: true);

        factory.Service.ActivateResult =
            Result<CommodityResponse>.Success(
                commodity);

        using var client =
            factory.CreateHttpsClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/v1/organizations/{organizationId}/" +
                $"commodities/{commodity.Id}/activate");

        var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityResponse>();

        Assert.NotNull(content);
        Assert.True(content!.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ShouldReturnInactiveCommodity()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        var commodity = CreateCommodityResponse(
            organizationId,
            Guid.NewGuid(),
            isActive: false);

        factory.Service.DeactivateResult =
            Result<CommodityResponse>.Success(
                commodity);

        using var client =
            factory.CreateHttpsClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/v1/organizations/{organizationId}/" +
                $"commodities/{commodity.Id}/deactivate");

        var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityResponse>();

        Assert.NotNull(content);
        Assert.False(content!.IsActive);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory =
            new CommodityApiFactory();

        var organizationId = Guid.NewGuid();

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/organizations/{organizationId}/" +
            "commodities");

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
        using var factory =
            new CommodityApiFactory();

        factory.Authorization.Granted = false;

        var organizationId = Guid.NewGuid();

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.MasterDataRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WithoutWritePermission_ShouldReturnForbidden()
    {
        using var factory =
            new CommodityApiFactory();

        factory.Authorization.Granted = false;

        var organizationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using var client = factory.CreateHttpsClient();

        var request = new CreateCommodityRequest(
            "PADI",
            "Padi",
            categoryId,
            "Oryza sativa",
            "Tanaman pangan");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodities",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.MasterDataWrite,
            factory.Authorization.LastPermission);
    }

    private static CommodityResponse
        CreateCommodityResponse(
            Guid organizationId,
            Guid categoryId,
            string code = "PADI",
            string name = "Padi",
            bool isActive = true)
    {
        return new CommodityResponse(
            Guid.NewGuid(),
            organizationId,
            code,
            name,
            categoryId,
            "Oryza sativa",
            "Tanaman pangan",
            isActive,
            DateTime.UtcNow,
            null);
    }

    private sealed class CommodityApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeCommodityService Service
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

                services.RemoveAll<ICommodityService>();

                services.AddSingleton<ICommodityService>(
                    Service);
            });
        }
    }

    private sealed class FakeCommodityService :
        ICommodityService
    {
        public Result<CommodityResponse> CreateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<IReadOnlyList<CommodityResponse>>
            GetAllResult
        {
            get;
            set;
        } = Result<IReadOnlyList<CommodityResponse>>
            .Success(
                Array.Empty<CommodityResponse>());

        public Result<CommodityResponse> GetByIdResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CommodityResponse> UpdateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CommodityResponse> ActivateResult
        {
            get;
            set;
        } = SuccessResponse(
            isActive: true);

        public Result<CommodityResponse> DeactivateResult
        {
            get;
            set;
        } = SuccessResponse(
            isActive: false);

        public Task<Result<CommodityResponse>>
            CreateAsync(
                Guid organizationId,
                CreateCommodityRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<Result<IReadOnlyList<CommodityResponse>>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAllResult);
        }

        public Task<Result<CommodityResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid commodityId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<CommodityResponse>>
            UpdateAsync(
                Guid organizationId,
                Guid commodityId,
                UpdateCommodityRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateResult);
        }

        public Task<Result<CommodityResponse>>
            ActivateAsync(
                Guid organizationId,
                Guid commodityId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivateResult);
        }

        public Task<Result<CommodityResponse>>
            DeactivateAsync(
                Guid organizationId,
                Guid commodityId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeactivateResult);
        }

        private static Result<CommodityResponse>
            SuccessResponse(
                bool isActive = true)
        {
            return Result<CommodityResponse>.Success(
                new CommodityResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "PADI",
                    "Padi",
                    Guid.NewGuid(),
                    "Oryza sativa",
                    null,
                    isActive,
                    DateTime.UtcNow,
                    null));
        }
    }
}
