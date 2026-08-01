using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Finance.Expenses;
using SiPacul.Application.Finance.Expenses.Contracts;
using SiPacul.Application.Finance.Expenses.Services;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.Expenses;

public sealed class CultivationExpenseEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid ExpenseId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly DateOnly ExpenseDate =
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
                    CultivationExpenseResponse>();

        Assert.NotNull(body);
        Assert.Equal(ExpenseId, body!.Id);

        Assert.Equal(
            $"{BasePath}/{ExpenseId}",
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
                Result<CultivationExpenseResponse>.Failure(
                    CultivationExpenseErrors.Validation(
                        "Invalid expense."))
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
                Result<CultivationExpenseResponse>.Failure(
                    CultivationExpenseErrors
                        .CodeAlreadyExists("EXP-001"))
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
            "&category=LandLease" +
            "&expenseDateFrom=2027-01-01" +
            "&expenseDateTo=2027-01-31" +
            "&payeeName=Pemilik%20Lahan";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            CultivationExpenseStatus.Confirmed,
            service.LastFilter!.Status);

        Assert.Equal(
            CultivationExpenseCategory.LandLease,
            service.LastFilter.Category);

        Assert.Equal(
            new DateOnly(2027, 1, 1),
            service.LastFilter.ExpenseDateFrom);

        Assert.Equal(
            new DateOnly(2027, 1, 31),
            service.LastFilter.ExpenseDateTo);

        Assert.Equal(
            "Pemilik Lahan",
            service.LastFilter.PayeeName);
    }

    [Fact]
    public async Task GetAll_WhenParentMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ListResult =
                Result<
                    IReadOnlyList<
                        CultivationExpenseResponse>>
                    .Failure(
                        CultivationExpenseErrors
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
            $"{BasePath}/{ExpenseId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            ExpenseId,
            service.LastExpenseId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<CultivationExpenseResponse>.Failure(
                    CultivationExpenseErrors.NotFound(
                        ExpenseId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{ExpenseId}");

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
            $"{BasePath}/{ExpenseId}",
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
                Result<CultivationExpenseResponse>.Failure(
                    CultivationExpenseErrors
                        .InvalidStatusTransition(
                            "Expense is immutable."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{ExpenseId}",
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
            $"{BasePath}/{ExpenseId}/confirm",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(1, service.ConfirmCallCount);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new CancelCultivationExpenseRequest(
                "Biaya dibatalkan");

        using var message =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"{BasePath}/{ExpenseId}/cancel")
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
    public async Task InvalidGuidRoute_ShouldReturnNotFound()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            "/api/v1/organizations/not-a-guid/" +
            $"crop-cycles/{CropCycleId}/expenses");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(0, service.TotalCallCount);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}/" +
        $"crop-cycles/{CropCycleId}/expenses";

    private static CreateCultivationExpenseRequest
        CreateRequest()
    {
        return new CreateCultivationExpenseRequest(
            "EXP-001",
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "Sewa lahan",
            1250000.13m,
            "Pemilik Lahan",
            "REF-001",
            "https://example.test/ref-001",
            "Transfer");
    }

    private static UpdateCultivationExpenseRequest
        UpdateRequest()
    {
        return new UpdateCultivationExpenseRequest(
            ExpenseDate.AddDays(1),
            CultivationExpenseCategory.Transport,
            "Transport panen",
            500000.13m,
            "Koperasi Angkut",
            "REF-002",
            null,
            "Tunai");
    }

    private static CultivationExpenseResponse
        CreateResponse()
    {
        return new CultivationExpenseResponse(
            ExpenseId,
            OrganizationId,
            CropCycleId,
            "EXP-001",
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "Sewa lahan",
            1250000.13m,
            "Pemilik Lahan",
            "REF-001",
            "https://example.test/ref-001",
            "Transfer",
            CultivationExpenseStatus.Draft,
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
        ICultivationExpenseService
    {
        public Result<CultivationExpenseResponse>
            SingleResult
        { get; set; } =
                Result<CultivationExpenseResponse>
                    .Success(CreateResponse());

        public Result<
            IReadOnlyList<CultivationExpenseResponse>>
            ListResult
        { get; set; } =
                Result<
                    IReadOnlyList<
                        CultivationExpenseResponse>>
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

        public Guid LastExpenseId
        {
            get;
            private set;
        }

        public CreateCultivationExpenseRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateCultivationExpenseRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public CancelCultivationExpenseRequest?
            LastCancelRequest
        {
            get;
            private set;
        }

        public CultivationExpenseFilter? LastFilter
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

        public Task<Result<CultivationExpenseResponse>>
            CreateAsync(
                Guid organizationId,
                Guid cropCycleId,
                CreateCultivationExpenseRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(organizationId, cropCycleId);
            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<
            Result<
                IReadOnlyList<
                    CultivationExpenseResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid cropCycleId,
                CultivationExpenseFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Capture(organizationId, cropCycleId);
            LastFilter = filter;

            return Task.FromResult(ListResult);
        }

        public Task<Result<CultivationExpenseResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid expenseId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                expenseId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationExpenseResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid expenseId,
                UpdateCultivationExpenseRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                expenseId);

            LastUpdateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationExpenseResponse>>
            ConfirmAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid expenseId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                expenseId);

            ConfirmCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<CultivationExpenseResponse>>
            CancelAsync(
                Guid organizationId,
                Guid cropCycleId,
                Guid expenseId,
                CancelCultivationExpenseRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                cropCycleId,
                expenseId);

            LastCancelRequest = request;

            return Task.FromResult(SingleResult);
        }

        private void Capture(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId = default)
        {
            TotalCallCount++;
            LastOrganizationId = organizationId;
            LastCropCycleId = cropCycleId;
            LastExpenseId = expenseId;
        }
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly ICultivationExpenseService
            _service;

        public ApiFactory(
            ICultivationExpenseService service)
        {
            _service = service;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    ICultivationExpenseService>();

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
