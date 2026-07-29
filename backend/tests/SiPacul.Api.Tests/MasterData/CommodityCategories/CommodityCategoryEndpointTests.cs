using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.MasterData.CommodityCategories;
using SiPacul.Application.MasterData.CommodityCategories.Contracts;
using SiPacul.Application.MasterData.CommodityCategories.Services;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.MasterData.CommodityCategories;

public sealed class CommodityCategoryEndpointTests
{
    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        var category = CreateCategoryResponse(
            organizationId);

        factory.Service.CreateResult =
            Result<CommodityCategoryResponse>.Success(
                category);

        using var client = factory.CreateHttpsClient();

        var request =
            new CreateCommodityCategoryRequest(
                "Tanaman Buah",
                "Tanaman penghasil buah");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodity-categories",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityCategoryResponse>();

        Assert.NotNull(content);
        Assert.Equal(category.Id, content!.Id);
        Assert.Equal(organizationId, content.OrganizationId);

        Assert.NotNull(response.Headers.Location);

        Assert.EndsWith(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodity-categories/{category.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.Validation(
                    "Commodity category name is invalid."));

        using var client = factory.CreateHttpsClient();

        var request =
            new CreateCommodityCategoryRequest(
                " ",
                null);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodity-categories",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CommodityCategoryErrors.ValidationCode,
            body);
    }

    [Fact]
    public async Task Create_WhenNameExists_ShouldReturnConflict()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NameAlreadyExists(
                    "Tanaman Buah"));

        using var client = factory.CreateHttpsClient();

        var request =
            new CreateCommodityCategoryRequest(
                "Tanaman Buah",
                null);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodity-categories",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            CommodityCategoryErrors.NameAlreadyExistsCode,
            body);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.CreateResult =
            Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.OrganizationNotFound(
                    organizationId));

        using var client = factory.CreateHttpsClient();

        var request =
            new CreateCommodityCategoryRequest(
                "Tanaman Buah",
                null);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodity-categories",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCategories()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        var category = CreateCategoryResponse(
            organizationId);

        factory.Service.GetAllResult =
            Result<
                IReadOnlyList<CommodityCategoryResponse>>
                .Success(
                    new[]
                    {
                        category
                    });

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            "commodity-categories");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityCategoryResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(category.Id, content![0].Id);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnCategory()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        var category = CreateCategoryResponse(
            organizationId);

        factory.Service.GetByIdResult =
            Result<CommodityCategoryResponse>.Success(
                category);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodity-categories/{category.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityCategoryResponse>();

        Assert.NotNull(content);
        Assert.Equal(category.Id, content!.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        factory.Service.GetByIdResult =
            Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NotFound(
                    organizationId,
                    categoryId));

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodity-categories/{categoryId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        var category = CreateCategoryResponse(
            organizationId,
            "Tanaman Perkebunan");

        factory.Service.UpdateResult =
            Result<CommodityCategoryResponse>.Success(
                category);

        using var client = factory.CreateHttpsClient();

        var request =
            new UpdateCommodityCategoryRequest(
                "Tanaman Perkebunan",
                "Kategori perkebunan");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/" +
            $"commodity-categories/{category.Id}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityCategoryResponse>();

        Assert.NotNull(content);

        Assert.Equal(
            "Tanaman Perkebunan",
            content!.Name);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnActiveCategory()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        var category = CreateCategoryResponse(
            organizationId,
            isActive: true);

        factory.Service.ActivateResult =
            Result<CommodityCategoryResponse>.Success(
                category);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"commodity-categories/{category.Id}/activate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityCategoryResponse>();

        Assert.NotNull(content);
        Assert.True(content!.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ShouldReturnInactiveCategory()
    {
        using var factory =
            new CommodityCategoryApiFactory();

        var organizationId = Guid.NewGuid();

        var category = CreateCategoryResponse(
            organizationId,
            isActive: false);

        factory.Service.DeactivateResult =
            Result<CommodityCategoryResponse>.Success(
                category);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/{organizationId}/" +
            $"commodity-categories/{category.Id}/deactivate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<
                CommodityCategoryResponse>();

        Assert.NotNull(content);
        Assert.False(content!.IsActive);
    }

    private static CommodityCategoryResponse
        CreateCategoryResponse(
            Guid organizationId,
            string name = "Tanaman Buah",
            bool isActive = true)
    {
        return new CommodityCategoryResponse(
            Guid.NewGuid(),
            organizationId,
            name,
            null,
            isActive,
            DateTime.UtcNow,
            null);
    }

    private sealed class CommodityCategoryApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeCommodityCategoryService Service
        {
            get;
        } = new();

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
                services.RemoveAll<
                    ICommodityCategoryService>();

                services.AddSingleton<
                    ICommodityCategoryService>(
                    Service);
            });
        }
    }

    private sealed class FakeCommodityCategoryService :
        ICommodityCategoryService
    {
        public Result<CommodityCategoryResponse>
            CreateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<
            IReadOnlyList<CommodityCategoryResponse>>
            GetAllResult
        {
            get;
            set;
        } = Result<
            IReadOnlyList<CommodityCategoryResponse>>
                .Success(
                    Array.Empty<
                        CommodityCategoryResponse>());

        public Result<CommodityCategoryResponse>
            GetByIdResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CommodityCategoryResponse>
            UpdateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<CommodityCategoryResponse>
            ActivateResult
        {
            get;
            set;
        } = SuccessResponse(
            isActive: true);

        public Result<CommodityCategoryResponse>
            DeactivateResult
        {
            get;
            set;
        } = SuccessResponse(
            isActive: false);

        public Task<Result<CommodityCategoryResponse>>
            CreateAsync(
                Guid organizationId,
                CreateCommodityCategoryRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<Result<
            IReadOnlyList<CommodityCategoryResponse>>>
            GetAllAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAllResult);
        }

        public Task<Result<CommodityCategoryResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid categoryId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<CommodityCategoryResponse>>
            UpdateAsync(
                Guid organizationId,
                Guid categoryId,
                UpdateCommodityCategoryRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateResult);
        }

        public Task<Result<CommodityCategoryResponse>>
            ActivateAsync(
                Guid organizationId,
                Guid categoryId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivateResult);
        }

        public Task<Result<CommodityCategoryResponse>>
            DeactivateAsync(
                Guid organizationId,
                Guid categoryId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeactivateResult);
        }

        private static Result<CommodityCategoryResponse>
            SuccessResponse(
                bool isActive = true)
        {
            return Result<CommodityCategoryResponse>.Success(
                new CommodityCategoryResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Tanaman Buah",
                    null,
                    isActive,
                    DateTime.UtcNow,
                    null));
        }
    }
}
