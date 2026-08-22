using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Evaluations.SeasonHistories;
using SiPacul.Application.Evaluations.SeasonHistories.Contracts;
using SiPacul.Application.Evaluations.SeasonHistories.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Evaluations.SeasonHistories;

public sealed class SeasonHistoryEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid LandId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid LandPlotId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Get_ShouldReturnPagedHistoryWithDefaults()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                LandSeasonHistoryResponse>();

        Assert.NotNull(body);
        Assert.Equal(OrganizationId, body!.OrganizationId);
        Assert.Equal(LandId, body.LandId);
        Assert.Equal("LAND-001", body.LandCode);
        Assert.Equal("Lahan Utama", body.LandName);
        Assert.Null(body.LandPlotId);
        Assert.False(body.IncludeNonTerminal);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        Assert.Empty(body.Seasons);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(LandId, service.LastLandId);
        Assert.NotNull(service.LastFilter);
        Assert.Null(service.LastFilter!.LandPlotId);
        Assert.False(service.LastFilter.IncludeNonTerminal);
        Assert.Equal(1, service.LastFilter.Page);
        Assert.Equal(20, service.LastFilter.PageSize);
        Assert.Equal(1, service.CallCount);

        Assert.Equal(
            OrganizationId,
            factory.Authorization.LastOrganizationId);

        Assert.Equal(
            Permissions.FinanceRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Get_WithQuery_ShouldPassCompleteFilter()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath +
            $"?landPlotId={LandPlotId}" +
            "&includeNonTerminal=true" +
            "&page=3" +
            "&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(service.LastFilter);
        Assert.Equal(
            LandPlotId,
            service.LastFilter!.LandPlotId);
        Assert.True(
            service.LastFilter.IncludeNonTerminal);
        Assert.Equal(3, service.LastFilter.Page);
        Assert.Equal(10, service.LastFilter.PageSize);
    }

    [Fact]
    public async Task Get_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            Result =
                Result<LandSeasonHistoryResponse>
                    .Failure(
                        SeasonHistoryErrors.Validation(
                            "Page must be at least one."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath + "?page=0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            SeasonHistoryErrors.ValidationCode,
            body);
    }

    [Fact]
    public async Task Get_WhenLandMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            Result =
                Result<LandSeasonHistoryResponse>
                    .Failure(
                        SeasonHistoryErrors.LandNotFound(
                            OrganizationId,
                            LandId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            SeasonHistoryErrors.LandNotFoundCode,
            body);
    }

    [Fact]
    public async Task Get_WhenPlotMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            Result =
                Result<LandSeasonHistoryResponse>
                    .Failure(
                        SeasonHistoryErrors.LandPlotNotFound(
                            LandId,
                            LandPlotId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath + $"?landPlotId={LandPlotId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            SeasonHistoryErrors.LandPlotNotFoundCode,
            body);
    }

    [Fact]
    public async Task Get_WhenSourceDataInvalid_ShouldReturnServerError()
    {
        var service = new StubService
        {
            Result =
                Result<LandSeasonHistoryResponse>
                    .Failure(
                        SeasonHistoryErrors.SourceDataInvalid(
                            "Cross-organization source row."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            SeasonHistoryErrors.SourceDataInvalidCode,
            body);
    }

    [Fact]
    public async Task Get_WithInvalidRouteGuid_ShouldReturnNotFound()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/v1/organizations/not-a-guid/" +
            "lands/not-a-guid/season-history");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(0, service.CallCount);
    }

    [Theory]
    [InlineData("landPlotId=not-a-guid")]
    [InlineData("includeNonTerminal=not-a-boolean")]
    [InlineData("page=not-a-number")]
    [InlineData("pageSize=not-a-number")]
    public async Task Get_WithMalformedQuery_ShouldReturnBadRequest(
        string query)
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath + "?" + query);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
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

        Assert.Equal(0, service.CallCount);
        Assert.Equal(0, factory.Authorization.CallCount);
    }

    [Fact]
    public async Task Get_WithoutFinanceRead_ShouldReturnForbidden()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(0, service.CallCount);

        Assert.Equal(
            OrganizationId,
            factory.Authorization.LastOrganizationId);

        Assert.Equal(
            Permissions.FinanceRead,
            factory.Authorization.LastPermission);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}/" +
        $"lands/{LandId}/season-history";

    private static LandSeasonHistoryResponse CreateResponse()
    {
        return new LandSeasonHistoryResponse(
            OrganizationId,
            LandId,
            "LAND-001",
            "Lahan Utama",
            null,
            null,
            null,
            false,
            1,
            20,
            0,
            0,
            false,
            false,
            Array.Empty<SeasonEvaluationResponse>(),
            new DateTime(
                2027,
                7,
                1,
                8,
                30,
                0,
                DateTimeKind.Utc));
    }

    private sealed class StubService : ISeasonHistoryService
    {
        public Result<LandSeasonHistoryResponse> Result
        { get; set; } =
            Result<LandSeasonHistoryResponse>
                .Success(CreateResponse());

        public Guid LastOrganizationId
        {
            get;
            private set;
        }

        public Guid LastLandId
        {
            get;
            private set;
        }

        public SeasonHistoryFilter? LastFilter
        {
            get;
            private set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<Result<LandSeasonHistoryResponse>> GetAsync(
            Guid organizationId,
            Guid landId,
            SeasonHistoryFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastOrganizationId = organizationId;
            LastLandId = landId;
            LastFilter = filter;
            CallCount++;

            return Task.FromResult(Result);
        }
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly ISeasonHistoryService _service;

        public ApiFactory(ISeasonHistoryService service)
        {
            _service = service;
        }

        public ConfigurableOrganizationPermissionService
            Authorization
        { get; } = new();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddOrganizationAuthorizationForTests(
                    Authorization);

                services.RemoveAll<ISeasonHistoryService>();
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
