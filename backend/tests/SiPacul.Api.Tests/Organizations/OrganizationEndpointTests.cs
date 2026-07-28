using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Organizations;
using SiPacul.Application.Organizations.Contracts;
using SiPacul.Application.Organizations.Services;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Organizations;

public sealed class OrganizationEndpointTests
{
    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreated()
    {
        using var factory =
            new OrganizationApiFactory();

        var organization =
            CreateOrganizationResponse();

        factory.Service.CreateResult =
            Result<OrganizationResponse>.Success(
                organization);

        using var client = factory.CreateHttpsClient();

        var request = new CreateOrganizationRequest(
            "ORG-001",
            "Bisnis Pertanian",
            null,
            "Asia/Jakarta");

        var response = await client.PostAsJsonAsync(
            "/api/v1/organizations",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<
                    OrganizationResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            organization.Id,
            content!.Id);

        Assert.NotNull(
            response.Headers.Location);

        Assert.EndsWith(
            $"/api/v1/organizations/{organization.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        using var factory =
            new OrganizationApiFactory();

        factory.Service.CreateResult =
            Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    "Organization code is invalid."));

        using var client = factory.CreateHttpsClient();

        var request = new CreateOrganizationRequest(
            "ORG 001",
            "Bisnis Pertanian",
            null,
            null);

        var response = await client.PostAsJsonAsync(
            "/api/v1/organizations",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            OrganizationErrors.ValidationCode,
            content);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldReturnConflict()
    {
        using var factory =
            new OrganizationApiFactory();

        factory.Service.CreateResult =
            Result<OrganizationResponse>.Failure(
                OrganizationErrors.CodeAlreadyExists(
                    "ORG-001"));

        using var client = factory.CreateHttpsClient();

        var request = new CreateOrganizationRequest(
            "ORG-001",
            "Bisnis Pertanian",
            null,
            null);

        var response = await client.PostAsJsonAsync(
            "/api/v1/organizations",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            OrganizationErrors.CodeAlreadyExistsCode,
            content);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOrganizations()
    {
        using var factory =
            new OrganizationApiFactory();

        var organization =
            CreateOrganizationResponse();

        factory.Service.GetAllResult =
            Result<IReadOnlyList<OrganizationResponse>>
                .Success(
                    new[]
                    {
                        organization
                    });

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/v1/organizations");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<
                    OrganizationResponse[]>();

        Assert.NotNull(content);
        Assert.Single(content!);
        Assert.Equal(
            organization.Id,
            content![0].Id);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOrganization()
    {
        using var factory =
            new OrganizationApiFactory();

        var organization =
            CreateOrganizationResponse();

        factory.Service.GetByIdResult =
            Result<OrganizationResponse>.Success(
                organization);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organization.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<
                    OrganizationResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            organization.Id,
            content!.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        using var factory =
            new OrganizationApiFactory();

        var organizationId = Guid.NewGuid();

        factory.Service.GetByIdResult =
            Result<OrganizationResponse>.Failure(
                OrganizationErrors.NotFound(
                    organizationId));

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/organizations/{organizationId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        using var factory =
            new OrganizationApiFactory();

        var organization =
            CreateOrganizationResponse(
                name: "Nama Baru");

        factory.Service.UpdateResult =
            Result<OrganizationResponse>.Success(
                organization);

        using var client = factory.CreateHttpsClient();

        var request = new UpdateOrganizationRequest(
            "Nama Baru",
            null,
            "Asia/Jakarta");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organization.Id}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<
                    OrganizationResponse>();

        Assert.NotNull(content);
        Assert.Equal(
            "Nama Baru",
            content!.Name);
    }

    [Fact]
    public async Task Activate_WhenSuccessful_ShouldReturnActiveOrganization()
    {
        using var factory =
            new OrganizationApiFactory();

        var organization =
            CreateOrganizationResponse(
                isActive: true);

        factory.Service.ActivateResult =
            Result<OrganizationResponse>.Success(
                organization);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/" +
            $"{organization.Id}/activate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<
                    OrganizationResponse>();

        Assert.NotNull(content);
        Assert.True(content!.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ShouldReturnInactiveOrganization()
    {
        using var factory =
            new OrganizationApiFactory();

        var organization =
            CreateOrganizationResponse(
                isActive: false);

        factory.Service.DeactivateResult =
            Result<OrganizationResponse>.Success(
                organization);

        using var client = factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/organizations/" +
            $"{organization.Id}/deactivate");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<
                    OrganizationResponse>();

        Assert.NotNull(content);
        Assert.False(content!.IsActive);
    }

    private static OrganizationResponse
        CreateOrganizationResponse(
            string name = "Bisnis Pertanian",
            bool isActive = true)
    {
        return new OrganizationResponse(
            Guid.NewGuid(),
            "ORG-001",
            name,
            null,
            "Asia/Jakarta",
            isActive,
            DateTime.UtcNow,
            null);
    }

    private sealed class OrganizationApiFactory :
        WebApplicationFactory<Program>
    {
        public FakeOrganizationService Service { get; } =
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
                services.RemoveAll<
                    IOrganizationService>();

                services.AddSingleton<
                    IOrganizationService>(
                    Service);
            });
        }
    }

    private sealed class FakeOrganizationService :
        IOrganizationService
    {
        public Result<OrganizationResponse> CreateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<
            IReadOnlyList<OrganizationResponse>>
            GetAllResult
        {
            get;
            set;
        } = Result<
            IReadOnlyList<OrganizationResponse>>
                .Success(
                    Array.Empty<
                        OrganizationResponse>());

        public Result<OrganizationResponse> GetByIdResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<OrganizationResponse> UpdateResult
        {
            get;
            set;
        } = SuccessResponse();

        public Result<OrganizationResponse> ActivateResult
        {
            get;
            set;
        } = SuccessResponse(
            isActive: true);

        public Result<OrganizationResponse> DeactivateResult
        {
            get;
            set;
        } = SuccessResponse(
            isActive: false);

        public Task<Result<OrganizationResponse>>
            CreateAsync(
                CreateOrganizationRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult);
        }

        public Task<Result<
            IReadOnlyList<OrganizationResponse>>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetAllResult);
        }

        public Task<Result<OrganizationResponse>>
            GetByIdAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<OrganizationResponse>>
            UpdateAsync(
                Guid organizationId,
                UpdateOrganizationRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateResult);
        }

        public Task<Result<OrganizationResponse>>
            ActivateAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActivateResult);
        }

        public Task<Result<OrganizationResponse>>
            DeactivateAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeactivateResult);
        }

        private static Result<OrganizationResponse>
            SuccessResponse(
                bool isActive = true)
        {
            return Result<OrganizationResponse>.Success(
                new OrganizationResponse(
                    Guid.NewGuid(),
                    "ORG-001",
                    "Bisnis Pertanian",
                    null,
                    "Asia/Jakarta",
                    isActive,
                    DateTime.UtcNow,
                    null));
        }
    }
}
