using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Previews;
using SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Previews.Services;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.ProfitSharing.Previews;

public sealed class ProfitSharingPreviewEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Get_ShouldReturnPreviewAndRequireRead()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.CallCount);
        Assert.Equal(OrganizationId, service.LastOrganizationId);
        Assert.Equal(CropCycleId, service.LastCropCycleId);
        Assert.Equal(
            Permissions.ProfitSharingRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Get_WhenAssignmentMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            Result = Result<ProfitSharingPreviewResponse>.Failure(
                ProfitSharingPreviewErrors.AssignmentNotFound(
                    CropCycleId))
        };
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenCalculationUnavailable_ShouldReturnConflict()
    {
        var service = new StubService
        {
            Result = Result<ProfitSharingPreviewResponse>.Failure(
                ProfitSharingPreviewErrors.CalculationUnavailable(
                    "Capital does not equal cultivation cost."))
        };
        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenForbidden_ShouldNotCallService()
    {
        var service = new StubService();
        using var factory = new ApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.CallCount);
    }

    private static string BasePath =>
        "/api/v1/organizations/" +
        $"{OrganizationId}/crop-cycles/{CropCycleId}/" +
        "profit-sharing-preview";

    private static ProfitSharingPreviewResponse CreateResponse()
    {
        var now = new DateTime(
            2027,
            7,
            24,
            8,
            0,
            0,
            DateTimeKind.Utc);

        var assignment = new ProfitSharingSchemeAssignmentResponse(
            Guid.NewGuid(),
            OrganizationId,
            CropCycleId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SCHEME-001",
            "Skema Uji",
            null,
            1,
            ProfitSharingResidualMethod.ProRataCapital,
            null,
            now,
            now,
            null,
            [],
            [],
            []);

        var profitability = new CropCycleProfitabilityResponse(
            OrganizationId,
            CropCycleId,
            "CYCLE-001",
            "Siklus Uji",
            Guid.NewGuid(),
            "CABAI",
            "Cabai",
            150_000m,
            150_000m,
            0m,
            100_000m,
            0m,
            100_000m,
            50_000m,
            33.3333m,
            ProfitabilityOutcome.Profit,
            80_000m,
            20_000m,
            100_000m,
            0m,
            0m,
            0m,
            null,
            now);

        return new ProfitSharingPreviewResponse(
            OrganizationId,
            CropCycleId,
            false,
            "SIPACUL-PS-2",
            now,
            assignment,
            profitability,
            new ProfitSharingPreviewTotalsResponse(
                100_000m,
                100_000m,
                0m,
                16_666.67m,
                0m,
                16_666.67m,
                33_333.33m,
                50_000m,
                150_000m,
                ProfitSharingResidualMethod.ProRataCapital),
            [],
            []);
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly IProfitSharingPreviewService _service;

        public ApiFactory(IProfitSharingPreviewService service)
        {
            _service = service;
        }

        public ConfigurableOrganizationPermissionService
            Authorization { get; } = new();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddOrganizationAuthorizationForTests(
                    Authorization);
                services.RemoveAll<IProfitSharingPreviewService>();
                services.AddSingleton(_service);
            });
        }

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });
        }
    }

    private sealed class StubService : IProfitSharingPreviewService
    {
        public Result<ProfitSharingPreviewResponse> Result { get; set; } =
            Result<ProfitSharingPreviewResponse>.Success(
                CreateResponse());

        public Guid LastOrganizationId { get; private set; }

        public Guid LastCropCycleId { get; private set; }

        public int CallCount { get; private set; }

        public Task<Result<ProfitSharingPreviewResponse>> GetAsync(
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
}
