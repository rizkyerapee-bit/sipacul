using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Finance.CapitalContributions;
using SiPacul.Application.Finance.CapitalContributions.Contracts;
using SiPacul.Application.Finance.CapitalContributions.Services;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.CapitalContributions;

public sealed class CapitalContributionEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid ContributionId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly DateOnly ContributionDate =
        new(2027, 1, 5);

    [Fact]
    public async Task Create_ShouldReturnCreatedAndLocation()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
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
                    CapitalContributionResponse>();

        Assert.NotNull(body);
        Assert.Equal(ContributionId, body!.Id);

        Assert.Equal(
            $"{BasePath}/{ContributionId}",
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
                Result<CapitalContributionResponse>.Failure(
                    CapitalContributionErrors.Validation(
                        "Invalid contribution."))
        };

        using var factory = new ApiFactory(service);
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
                Result<CapitalContributionResponse>.Failure(
                    CapitalContributionErrors
                        .CodeAlreadyExists("CAP-001"))
        };

        using var factory = new ApiFactory(service);
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

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var path =
            BasePath +
            "?status=Confirmed" +
            "&contributorRole=Investor" +
            "&contributionDateFrom=2027-01-01" +
            "&contributionDateTo=2027-01-31" +
            "&contributorCode=INV-001" +
            "&contributorName=Investor%20Utama";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            CapitalContributionStatus.Confirmed,
            service.LastFilter!.Status);

        Assert.Equal(
            CapitalContributorRole.Investor,
            service.LastFilter.ContributorRole);

        Assert.Equal(
            new DateOnly(2027, 1, 1),
            service.LastFilter.ContributionDateFrom);

        Assert.Equal(
            new DateOnly(2027, 1, 31),
            service.LastFilter.ContributionDateTo);

        Assert.Equal(
            "INV-001",
            service.LastFilter.ContributorCode);

        Assert.Equal(
            "Investor Utama",
            service.LastFilter.ContributorName);
    }

    [Fact]
    public async Task GetAll_WhenParentMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ListResult =
                Result<
                    IReadOnlyList<
                        CapitalContributionResponse>>
                    .Failure(
                        CapitalContributionErrors
                            .CropCycleNotFound(
                                CropCycleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkAndBindIdentifiers()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{ContributionId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            ContributionId,
            service.LastContributionId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CapitalContributionResponse>.Failure(
                    CapitalContributionErrors.NotFound(
                        ContributionId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{ContributionId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request = UpdateRequest();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{ContributionId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastUpdateRequest);
    }

    [Fact]
    public async Task UpdateDraft_WhenImmutable_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CapitalContributionResponse>.Failure(
                    CapitalContributionErrors
                        .InvalidStatusTransition(
                            "Contribution is immutable."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{ContributionId}",
            UpdateRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Confirm_ShouldReturnOkAndInvokeService()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{ContributionId}/confirm",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(1, service.ConfirmCallCount);
    }

    [Fact]
    public async Task Confirm_WhenImmutable_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CapitalContributionResponse>.Failure(
                    CapitalContributionErrors
                        .InvalidStatusTransition(
                            "Contribution is already confirmed."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{ContributionId}/confirm",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new CancelCapitalContributionRequest(
                "Kontribusi dibatalkan");

        using var message =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"{BasePath}/{ContributionId}/cancel")
            {
                Content = JsonContent.Create(request)
            };

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastCancelRequest);
    }

    [Fact]
    public async Task Cancel_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CapitalContributionResponse>.Failure(
                    CapitalContributionErrors.Validation(
                        "Cancellation reason is required."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        using var message =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"{BasePath}/{ContributionId}/cancel")
            {
                Content = JsonContent.Create(
                    new CancelCapitalContributionRequest(
                        " "))
            };

        var response = await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task InvalidGuidRoute_ShouldReturnNotFound()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/v1/organizations/not-a-guid/" +
            $"crop-cycles/{CropCycleId}/" +
            "capital-contributions");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(0, service.TotalCallCount);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}/" +
        $"crop-cycles/{CropCycleId}/" +
        "capital-contributions";

    private static CreateCapitalContributionRequest
        CreateRequest()
    {
        return new CreateCapitalContributionRequest(
            "CAP-001",
            ContributionDate,
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor,
            10000000.13m,
            CapitalContributionPaymentMethod.BankTransfer,
            "TRF-001",
            "Modal tahap pertama");
    }

    private static UpdateCapitalContributionRequest
        UpdateRequest()
    {
        return new UpdateCapitalContributionRequest(
            ContributionDate.AddDays(1),
            "MITRA-001",
            "Mitra Pengelola",
            CapitalContributorRole.Partner,
            2500000.13m,
            CapitalContributionPaymentMethod.Cash,
            "CASH-002",
            "Modal Mitra");
    }

    private static CapitalContributionResponse
        CreateResponse()
    {
        return new CapitalContributionResponse(
            ContributionId,
            OrganizationId,
            CropCycleId,
            "CAP-001",
            ContributionDate,
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor,
            10000000.13m,
            CapitalContributionPaymentMethod.BankTransfer,
            "TRF-001",
            "Modal tahap pertama",
            CapitalContributionStatus.Draft,
            false,
            true,
            false,
            null,
            null,
            new DateTime(
                2027,
                1,
                5,
                8,
                0,
                0,
                DateTimeKind.Utc),
            null);
    }

    private sealed class StubService :
        ICapitalContributionService
    {
        public Result<CapitalContributionResponse>
            SingleResult
        { get; set; } =
                Result<CapitalContributionResponse>
                    .Success(CreateResponse());

        public Result<
            IReadOnlyList<CapitalContributionResponse>>
            ListResult
        { get; set; } =
                Result<
                    IReadOnlyList<
                        CapitalContributionResponse>>
                    .Success(
                        new[]
                        {
                            CreateResponse()
                        });

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

        public Guid LastContributionId
        {
            get;
            private set;
        }

        public CreateCapitalContributionRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateCapitalContributionRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public CancelCapitalContributionRequest?
            LastCancelRequest
        {
            get;
            private set;
        }

        public CapitalContributionFilter? LastFilter
        {
            get;
            private set;
        }

        public int ConfirmCallCount
        {
            get;
            private set;
        }

        public int TotalCallCount
        {
            get;
            private set;
        }

        public Task<Result<CapitalContributionResponse>>
            CreateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CreateCapitalContributionRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId);

            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<
            Result<
                IReadOnlyList<
                    CapitalContributionResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                CapitalContributionFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId);

            LastFilter = filter;

            return Task.FromResult(ListResult);
        }

        public Task<Result<CapitalContributionResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid contributionId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                contributionId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CapitalContributionResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid contributionId,
                UpdateCapitalContributionRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                contributionId);

            LastUpdateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CapitalContributionResponse>>
            ConfirmAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid contributionId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                contributionId);

            ConfirmCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CapitalContributionResponse>>
            CancelAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid contributionId,
                CancelCapitalContributionRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                contributionId);

            LastCancelRequest = request;

            return Task.FromResult(SingleResult);
        }

        private void Capture(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId = default)
        {
            TotalCallCount++;
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            LastContributionId = contributionId;
        }
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly ICapitalContributionService
            _service;

        public ApiFactory(
            ICapitalContributionService service)
        {
            _service = service;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    ICapitalContributionService>();

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
