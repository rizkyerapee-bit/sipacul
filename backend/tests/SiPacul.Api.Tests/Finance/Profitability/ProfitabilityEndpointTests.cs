using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Finance.Profitability;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Finance.Profitability.Services;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.Profitability;

public sealed class ProfitabilityEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Get_ShouldReturnCompleteReport()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    CropCycleProfitabilityResponse>();

        Assert.NotNull(body);
        Assert.Equal(OrganizationId, body!.OrganizationId);
        Assert.Equal(CropCycleId, body.CropCycleId);
        Assert.Equal("CC-001", body.CropCycleCode);
        Assert.Equal("Musim Padi", body.CropCycleName);
        Assert.Equal(CommodityId, body.CommodityIdSnapshot);
        Assert.Equal("PADI", body.CommodityCodeSnapshot);
        Assert.Equal("Padi", body.CommodityNameSnapshot);
        Assert.Equal(2000000m, body.RecognizedRevenue);
        Assert.Equal(1500000m, body.CollectedRevenue);
        Assert.Equal(500000m, body.OutstandingReceivable);
        Assert.Equal(600000m, body.ActivityResourceCost);
        Assert.Equal(400000m, body.ManualExpenseCost);
        Assert.Equal(1000000m, body.TotalCultivationCost);
        Assert.Equal(1000000m, body.NetProfit);
        Assert.Equal(50m, body.ProfitMarginPercentage);

        Assert.Equal(
            ProfitabilityOutcome.Profit,
            body.Outcome);

        Assert.Equal(700000m, body.ConfirmedInvestorCapital);
        Assert.Equal(300000m, body.ConfirmedPartnerCapital);
        Assert.Equal(1000000m, body.TotalConfirmedCapital);
        Assert.Equal(0m, body.CapitalFundingGap);
        Assert.Equal(0m, body.CapitalFundingExcess);
        Assert.Equal(125.5m, body.AvailableHarvestQuantity);

        Assert.Equal(
            HarvestQuantityUnit.Kilogram,
            body.HarvestQuantityUnit);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(
            CropCycleId,
            service.LastCropCycleId);

        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task Get_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            Result =
                Result<CropCycleProfitabilityResponse>
                    .Failure(
                        ProfitabilityErrors.Validation(
                            "Invalid identifier."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            Result =
                Result<CropCycleProfitabilityResponse>
                    .Failure(
                        ProfitabilityErrors
                            .OrganizationNotFound(
                                OrganizationId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenCropCycleMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            Result =
                Result<CropCycleProfitabilityResponse>
                    .Failure(
                        ProfitabilityErrors
                            .CropCycleNotFound(
                                CropCycleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenSourcesInvalid_ShouldReturnConflict()
    {
        var service = new StubService
        {
            Result =
                Result<CropCycleProfitabilityResponse>
                    .Failure(
                        ProfitabilityErrors.SourceDataInvalid(
                            "Mixed harvest units."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_WithInvalidRouteGuid_ShouldReturnNotFound()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response =
            await client.GetAsync(
                "/api/v1/organizations/not-a-guid/" +
                "crop-cycles/not-a-guid/profitability");

        Assert.Equal(
            HttpStatusCode.NotFound,
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

        Assert.Equal(0, factory.Authorization.CallCount);
    }

    [Fact]
    public async Task Get_WithoutReadPermission_ShouldReturnForbidden()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
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
            Permissions.FinanceRead,
            factory.Authorization.LastPermission);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}/" +
        $"crop-cycles/{CropCycleId}/profitability";

    private static CropCycleProfitabilityResponse
        CreateResponse()
    {
        return new CropCycleProfitabilityResponse(
            OrganizationId,
            CropCycleId,
            "CC-001",
            "Musim Padi",
            CommodityId,
            "PADI",
            "Padi",
            2000000m,
            1500000m,
            500000m,
            600000m,
            400000m,
            1000000m,
            1000000m,
            50m,
            ProfitabilityOutcome.Profit,
            700000m,
            300000m,
            1000000m,
            0m,
            0m,
            125.5m,
            HarvestQuantityUnit.Kilogram,
            new DateTime(
                2027,
                7,
                1,
                8,
                30,
                0,
                DateTimeKind.Utc));
    }

    private sealed class StubService :
        IProfitabilityService
    {
        public Result<CropCycleProfitabilityResponse>
            Result
        { get; set; } =
                Result<CropCycleProfitabilityResponse>
                    .Success(CreateResponse());

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

        public int CallCount
        {
            get;
            private set;
        }

        public Task<
            Result<CropCycleProfitabilityResponse>>
            GetCropCycleReportAsync(
                Guid organizationId,
                Guid cropCycleId,
                CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            CallCount++;

            return Task.FromResult(Result);
        }
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly IProfitabilityService _service;

        public ApiFactory(
            IProfitabilityService service)
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

                services.RemoveAll<IProfitabilityService>();
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
