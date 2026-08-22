using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Evaluations.SeasonReviews;
using SiPacul.Application.Evaluations.SeasonReviews.Contracts;
using SiPacul.Application.Evaluations.SeasonReviews.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Shared.Results;

namespace SiPacul.Api.Tests.Evaluations.SeasonReviews;

public sealed class SeasonReviewEndpointTests
{
    private static readonly Guid OrganizationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ReviewId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid CropCycleId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static string BasePath => $"/api/v1/organizations/{OrganizationId}/season-reviews";

    [Fact]
    public async Task Create_ReturnsCreatedAndRequiresWrite()
    {
        var service = new StubService(); using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        var request = new CreateSeasonReviewRequest(CropCycleId, new DateOnly(2026, 8, 22), "Findings", "Lessons", "Recommendations");
        var response = await client.PostAsJsonAsync(BasePath, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains($"/{ReviewId}", response.Headers.Location?.ToString());
        Assert.Equal(OrganizationId, service.LastOrganizationId); Assert.Equal(CropCycleId, service.LastCropCycleId);
        Assert.Equal(Permissions.CultivationWrite, factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task GetById_ReturnsOkAndRequiresRead()
    {
        var service = new StubService(); using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync($"{BasePath}/{ReviewId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(ReviewId, service.LastReviewId);
        Assert.Equal(Permissions.CultivationRead, factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task GetByCropCycle_ForwardsCycleAndRequiresRead()
    {
        var service = new StubService(); using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync($"{BasePath}/by-crop-cycle/{CropCycleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(CropCycleId, service.LastCropCycleId);
        Assert.Equal(Permissions.CultivationRead, factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Update_ForwardsBodyAndRequiresWrite()
    {
        var service = new StubService(); using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        var request = new UpdateSeasonReviewRequest(new DateOnly(2026, 8, 23), "New findings", "New lessons", "New recommendations");
        var response = await client.PutAsJsonAsync($"{BasePath}/{ReviewId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("New findings", service.LastUpdate?.Findings);
        Assert.Equal(Permissions.CultivationWrite, factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Finalize_ForwardsIdAndRequiresWrite()
    {
        var service = new StubService(); using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BasePath}/{ReviewId}/finalize");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(ReviewId, service.LastReviewId);
        Assert.Equal(Permissions.CultivationWrite, factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WhenDuplicate_ReturnsConflict()
    {
        var service = new StubService { Result = Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.AlreadyExists(CropCycleId)) };
        using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        var response = await client.PostAsJsonAsync(BasePath, new CreateSeasonReviewRequest(CropCycleId, new DateOnly(2026, 8, 22), "F", "L", "R"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(SeasonReviewErrors.AlreadyExistsCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        var service = new StubService(); using var factory = new ApiFactory(service); using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BasePath}/{ReviewId}");
        request.Headers.Add(OrganizationAuthorizationTestSupport.UnauthenticatedHeaderName, "true");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Equal(0, service.CallCount);
    }

    [Theory]
    [InlineData("GET", false)]
    [InlineData("POST", true)]
    public async Task WithoutPermission_ReturnsForbidden(string method, bool write)
    {
        var service = new StubService(); using var factory = new ApiFactory(service); factory.Authorization.Granted = false; using var client = factory.CreateHttpsClient();
        HttpResponseMessage response = method == "GET"
            ? await client.GetAsync($"{BasePath}/{ReviewId}")
            : await client.PostAsJsonAsync(BasePath, new CreateSeasonReviewRequest(CropCycleId, new DateOnly(2026, 8, 22), "F", "L", "R"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(0, service.CallCount);
        Assert.Equal(write ? Permissions.CultivationWrite : Permissions.CultivationRead, factory.Authorization.LastPermission);
    }

    private static SeasonReviewResponse Response() => new(ReviewId, OrganizationId, CropCycleId,
        new DateOnly(2026, 8, 22), "Findings", "Lessons", "Recommendations", SeasonReviewStatus.Draft,
        null, new DateTime(2026, 8, 22, 8, 0, 0, DateTimeKind.Utc), null);

    private sealed class StubService : ISeasonReviewService
    {
        public Result<SeasonReviewResponse> Result { get; set; } = Result<SeasonReviewResponse>.Success(Response());
        public Guid LastOrganizationId { get; private set; } public Guid LastReviewId { get; private set; }
        public Guid LastCropCycleId { get; private set; } public UpdateSeasonReviewRequest? LastUpdate { get; private set; }
        public int CallCount { get; private set; }
        private Task<Result<SeasonReviewResponse>> Record(Guid organizationId, Guid reviewId=default, Guid cropCycleId=default, UpdateSeasonReviewRequest? update=null)
        { LastOrganizationId=organizationId;LastReviewId=reviewId;LastCropCycleId=cropCycleId;LastUpdate=update;CallCount++;return Task.FromResult(Result); }
        public Task<Result<SeasonReviewResponse>> CreateAsync(Guid organizationId, CreateSeasonReviewRequest request, CancellationToken cancellationToken=default)=>Record(organizationId,cropCycleId:request.CropCycleId);
        public Task<Result<SeasonReviewResponse>> GetByIdAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken=default)=>Record(organizationId,reviewId);
        public Task<Result<SeasonReviewResponse>> GetByCropCycleAsync(Guid organizationId, Guid cropCycleId, CancellationToken cancellationToken=default)=>Record(organizationId,cropCycleId:cropCycleId);
        public Task<Result<SeasonReviewResponse>> UpdateAsync(Guid organizationId, Guid reviewId, UpdateSeasonReviewRequest request, CancellationToken cancellationToken=default)=>Record(organizationId,reviewId,update:request);
        public Task<Result<SeasonReviewResponse>> FinalizeAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken=default)=>Record(organizationId,reviewId);
    }

    private sealed class ApiFactory(ISeasonReviewService service) : WebApplicationFactory<Program>
    {
        public ConfigurableOrganizationPermissionService Authorization { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureTestServices(services =>
        { services.AddOrganizationAuthorizationForTests(Authorization); services.RemoveAll<ISeasonReviewService>(); services.AddSingleton(service); });
        public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }
}
