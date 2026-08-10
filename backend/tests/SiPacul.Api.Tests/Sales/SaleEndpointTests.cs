using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Api.Security;
using SiPacul.Api.Tests.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Sales;
using SiPacul.Application.Sales.Contracts;
using SiPacul.Application.Sales.Services;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Sales;

public sealed class SaleEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid SaleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid SaleLineId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly Guid HarvestBatchId =
        Guid.Parse(
            "40000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "50000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "60000000-0000-0000-0000-000000000001");

    private static readonly DateOnly SaleDate =
        new(2027, 5, 10);

    [Fact]
    public async Task Create_ShouldReturnCreatedAndLocation()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = CreateRequest();

        var response = await client.PostAsJsonAsync(
            BasePath,
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<SaleResponse>();

        Assert.NotNull(body);
        Assert.Equal(SaleId, body!.Id);

        Assert.Equal(
            $"{BasePath}/{SaleId}",
            response.Headers.Location?.AbsolutePath);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

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
                Result<SaleResponse>.Failure(
                    SaleErrors.Validation(
                        "Invalid sale."))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

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
                Result<SaleResponse>.Failure(
                    SaleErrors.CodeAlreadyExists(
                        "SALE-001"))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAuthenticationCookieAndWithoutAntiforgery_ShouldReturnBadRequest()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BasePath)
        {
            Content = JsonContent.Create(
                CreateRequest())
        };

        AddAuthenticationCookie(request);

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Null(service.LastCreateRequest);
    }

    [Fact]
    public async Task GetAll_ShouldBindFiltersAndReturnOk()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var path =
            BasePath +
            "?status=Confirmed" +
            "&saleDateFrom=2027-05-01" +
            "&saleDateTo=2027-05-31" +
            "&paymentTerm=Credit" +
            "&buyerName=Koperasi%20Tani";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    List<SaleResponse>>();

        Assert.NotNull(body);
        Assert.Single(body!);
        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            SaleStatus.Confirmed,
            service.LastFilter!.Status);

        Assert.Equal(
            new DateOnly(2027, 5, 1),
            service.LastFilter.SaleDateFrom);

        Assert.Equal(
            new DateOnly(2027, 5, 31),
            service.LastFilter.SaleDateTo);

        Assert.Equal(
            SalePaymentTerm.Credit,
            service.LastFilter.PaymentTerm);

        Assert.Equal(
            "Koperasi Tani",
            service.LastFilter.BuyerName);
    }

    [Fact]
    public async Task GetAll_WhenOrganizationMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ListResult =
                Result<IReadOnlyList<SaleResponse>>
                    .Failure(
                        SaleErrors.OrganizationNotFound(
                            OrganizationId))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            BasePath);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuthenticationCookieAndWithoutAntiforgery_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BasePath);

        AddAuthenticationCookie(request);

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkAndBindIdentifiers()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{SaleId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(
            SaleId,
            service.LastSaleId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.NotFound(SaleId))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{SaleId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateDraft_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = UpdateRequest();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{SaleId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastUpdateRequest);

        Assert.Equal(
            SaleId,
            service.LastSaleId);
    }

    [Fact]
    public async Task UpdateDraft_WhenImmutable_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.InvalidStatusTransition(
                        "Only draft sale can change."))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{SaleId}",
            UpdateRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task AddLine_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = AddLineRequest();

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/{SaleId}/lines",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastAddLineRequest);

        Assert.Equal(
            SaleId,
            service.LastSaleId);
    }

    [Fact]
    public async Task AddLine_WhenHarvestMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.HarvestBatchNotFound(
                        HarvestBatchId))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/{SaleId}/lines",
            AddLineRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task AddLine_WhenInsufficientQuantity_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.InsufficientQuantity(
                        HarvestBatchId,
                        100,
                        50))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/{SaleId}/lines",
            AddLineRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateLine_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request =
            new UpdateSaleLineRequest(
                25,
                2600,
                100,
                "Updated line");

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{SaleId}/lines/{SaleLineId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastUpdateLineRequest);

        Assert.Equal(
            SaleLineId,
            service.LastSaleLineId);
    }

    [Fact]
    public async Task UpdateLine_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.LineNotFound(
                        SaleId,
                        SaleLineId))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{SaleId}/lines/{SaleLineId}",
            new UpdateSaleLineRequest(
                10,
                2000,
                0,
                null));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task RemoveLine_ShouldReturnOkAndBindIdentifiers()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.DeleteAsync(
            $"{BasePath}/{SaleId}/lines/{SaleLineId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            SaleId,
            service.LastSaleId);

        Assert.Equal(
            SaleLineId,
            service.LastSaleLineId);

        Assert.Equal(
            1,
            service.RemoveLineCallCount);
    }

    [Fact]
    public async Task Confirm_ShouldReturnOk()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{BasePath}/{SaleId}/confirm");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            SaleId,
            service.LastSaleId);

        Assert.Equal(
            1,
            service.ConfirmCallCount);
    }

    [Fact]
    public async Task Confirm_WhenConcurrencyChanges_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.ConfirmationConcurrency())
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{BasePath}/{SaleId}/confirm");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var request =
            new CancelSaleRequest(
                "Buyer cancelled");

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{SaleId}/cancel",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            request,
            service.LastCancelRequest);

        Assert.Equal(
            SaleId,
            service.LastSaleId);
    }

    [Fact]
    public async Task Cancel_WhenReasonInvalid_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SaleResponse>.Failure(
                    SaleErrors.Validation(
                        "Cancellation reason required."))
        };

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{SaleId}/cancel",
            new CancelSaleRequest(""));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task InvalidSaleGuid_ShouldNotReachService()
    {
        var service = new StubService();

        using var factory =
            new SaleApiFactory(service);

        using var client =
            factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/not-a-guid");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            Guid.Empty,
            service.LastSaleId);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var service = new StubService();
        using var factory = new SaleApiFactory(service);
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
    public async Task GetAll_WithoutReadPermission_ShouldReturnForbidden()
    {
        var service = new StubService();
        using var factory = new SaleApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(BasePath);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.SalesRead,
            factory.Authorization.LastPermission);
    }

    [Fact]
    public async Task Create_WithoutWritePermission_ShouldReturnForbidden()
    {
        var service = new StubService();
        using var factory = new SaleApiFactory(service);
        factory.Authorization.Granted = false;
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            Permissions.SalesWrite,
            factory.Authorization.LastPermission);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}/sales";

    private static void AddAuthenticationCookie(
        HttpRequestMessage request)
    {
        request.Headers.Add(
            "Cookie",
            SiPaculAuthenticationDefaults
                .AuthenticationCookieName +
            "=test-authentication-cookie");
    }

    private static CreateSaleRequest CreateRequest()
    {
        return new CreateSaleRequest(
            "SALE-2027-0001",
            SaleDate,
            "Koperasi Tani",
            "08123456789",
            "Jl. Pertanian 10",
            SalePaymentTerm.Cash,
            null,
            "Penjualan panen");
    }

    private static UpdateSaleRequest UpdateRequest()
    {
        return new UpdateSaleRequest(
            SaleDate.AddDays(1),
            "Pedagang Besar",
            "089999999",
            "Pasar Induk",
            SalePaymentTerm.Credit,
            SaleDate.AddDays(30),
            500,
            "Kredit 30 hari");
    }

    private static AddSaleLineRequest AddLineRequest()
    {
        return new AddSaleLineRequest(
            HarvestBatchId,
            20,
            HarvestQuantityUnit.Kilogram,
            2500,
            100,
            "Grade A");
    }

    private static SaleResponse CreateResponse(
        SaleStatus status = SaleStatus.Draft)
    {
        DateTime? confirmedAt =
            status == SaleStatus.Confirmed
                ? new DateTime(
                    2027,
                    5,
                    10,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)
                : null;

        var line =
            new SaleLineResponse(
                SaleLineId,
                HarvestBatchId,
                "HRV-001",
                CropCycleId,
                "CC-001",
                CommodityId,
                "PADI",
                "Padi",
                "Grade A",
                20,
                HarvestQuantityUnit.Kilogram,
                2500,
                100,
                49900,
                "Sale line",
                new DateTime(
                    2027,
                    5,
                    10,
                    7,
                    0,
                    0,
                    DateTimeKind.Utc),
                null);

        return new SaleResponse(
            SaleId,
            OrganizationId,
            "SALE-2027-0001",
            SaleDate,
            "Koperasi Tani",
            "08123456789",
            "Jl. Pertanian 10",
            SalePaymentTerm.Cash,
            null,
            0,
            49900,
            49900,
            status,
            confirmedAt,
            null,
            "Penjualan panen",
            new[] { line },
            new DateTime(
                2027,
                5,
                10,
                7,
                0,
                0,
                DateTimeKind.Utc),
            null);
    }

    private sealed class StubService :
        ISaleService
    {
        public Result<SaleResponse>
            SingleResult
        { get; set; } =
                Result<SaleResponse>.Success(
                    CreateResponse());

        public Result<IReadOnlyList<SaleResponse>>
            ListResult
        { get; set; } =
                Result<IReadOnlyList<SaleResponse>>
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

        public Guid LastSaleId
        {
            get;
            private set;
        }

        public Guid LastSaleLineId
        {
            get;
            private set;
        }

        public SaleFilter? LastFilter
        {
            get;
            private set;
        }

        public CreateSaleRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateSaleRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public AddSaleLineRequest?
            LastAddLineRequest
        {
            get;
            private set;
        }

        public UpdateSaleLineRequest?
            LastUpdateLineRequest
        {
            get;
            private set;
        }

        public CancelSaleRequest?
            LastCancelRequest
        {
            get;
            private set;
        }

        public int RemoveLineCallCount
        {
            get;
            private set;
        }

        public int ConfirmCallCount
        {
            get;
            private set;
        }

        public Task<Result<SaleResponse>> CreateAsync(
            Guid organizationId,
            CreateSaleRequest request,
            CancellationToken cancellationToken = default)
        {
            Capture(organizationId);

            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<IReadOnlyList<SaleResponse>>>
            GetAllAsync(
                Guid organizationId,
                SaleFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Capture(organizationId);

            LastFilter = filter;

            return Task.FromResult(ListResult);
        }

        public Task<Result<SaleResponse>> GetByIdAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid saleId,
                UpdateSaleRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId);

            LastUpdateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleResponse>> AddLineAsync(
            Guid organizationId,
            Guid saleId,
            AddSaleLineRequest request,
            CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId);

            LastAddLineRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleResponse>> UpdateLineAsync(
            Guid organizationId,
            Guid saleId,
            Guid saleLineId,
            UpdateSaleLineRequest request,
            CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId,
                saleLineId);

            LastUpdateLineRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleResponse>> RemoveLineAsync(
            Guid organizationId,
            Guid saleId,
            Guid saleLineId,
            CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId,
                saleLineId);

            RemoveLineCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleResponse>> ConfirmAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId);

            ConfirmCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleResponse>> CancelAsync(
            Guid organizationId,
            Guid saleId,
            CancelSaleRequest request,
            CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId);

            LastCancelRequest = request;

            return Task.FromResult(SingleResult);
        }

        private void Capture(
            Guid organizationId,
            Guid saleId = default,
            Guid saleLineId = default)
        {
            LastOrganizationId = organizationId;
            LastSaleId = saleId;
            LastSaleLineId = saleLineId;
        }
    }

    private sealed class SaleApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly ISaleService _service;

        public SaleApiFactory(ISaleService service)
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

                services.RemoveAll<ISaleService>();
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
