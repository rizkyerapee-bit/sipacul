using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiPacul.Application.Finance.SalePayments;
using SiPacul.Application.Finance.SalePayments.Contracts;
using SiPacul.Application.Finance.SalePayments.Services;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Shared.Results;
using Xunit;

namespace SiPacul.Api.Tests.Finance.SalePayments;

public sealed class SalePaymentEndpointTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid SaleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid PaymentId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly DateOnly SaleDate =
        new(2027, 5, 10);

    private static readonly DateOnly PaymentDate =
        new(2027, 5, 12);

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
                .ReadFromJsonAsync<SalePaymentResponse>();

        Assert.NotNull(body);
        Assert.Equal(PaymentId, body!.Id);

        Assert.Equal(
            $"{BasePath}/{PaymentId}",
            response.Headers.Location?.AbsolutePath);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(SaleId, service.LastSaleId);
        Assert.Equal(request, service.LastCreateRequest);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.Validation(
                        "Invalid payment."))
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
    public async Task Create_WhenSaleMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.SaleNotFound(
                        SaleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            BasePath,
            CreateRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenSaleNotConfirmed_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.SaleNotConfirmed(
                        SaleId))
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
    public async Task Create_WhenCodeExists_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.CodeAlreadyExists(
                        "PAY-001"))
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
            "&paymentMethod=BankTransfer" +
            "&paymentDateFrom=2027-05-01" +
            "&paymentDateTo=2027-05-31" +
            "&receivedFrom=Koperasi%20Tani";

        var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(service.LastFilter);

        Assert.Equal(
            SalePaymentStatus.Confirmed,
            service.LastFilter!.Status);

        Assert.Equal(
            SalePaymentMethod.BankTransfer,
            service.LastFilter.PaymentMethod);

        Assert.Equal(
            new DateOnly(2027, 5, 1),
            service.LastFilter.PaymentDateFrom);

        Assert.Equal(
            new DateOnly(2027, 5, 31),
            service.LastFilter.PaymentDateTo);

        Assert.Equal(
            "Koperasi Tani",
            service.LastFilter.ReceivedFrom);
    }

    [Fact]
    public async Task GetAll_WhenSaleMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ListResult =
                Result<
                    IReadOnlyList<SalePaymentResponse>>
                    .Failure(
                        SalePaymentErrors.SaleNotFound(
                            SaleId))
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
            $"{BasePath}/{PaymentId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            OrganizationId,
            service.LastOrganizationId);

        Assert.Equal(SaleId, service.LastSaleId);
        Assert.Equal(PaymentId, service.LastPaymentId);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.NotFound(
                        PaymentId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/{PaymentId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnOkAndBindRequest()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request = UpdateRequest();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{PaymentId}",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(PaymentId, service.LastPaymentId);
        Assert.Equal(request, service.LastUpdateRequest);
    }

    [Fact]
    public async Task Update_WhenStatusInvalid_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors
                        .InvalidStatusTransition(
                            "Only Draft can be updated."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{PaymentId}",
            UpdateRequest());

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Confirm_ShouldReturnOkAndCallService()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{PaymentId}/confirm",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(1, service.ConfirmCallCount);
        Assert.Equal(PaymentId, service.LastPaymentId);
    }

    [Fact]
    public async Task Confirm_WhenOverpayment_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.Overpayment(
                        1000000.01m,
                        1000000m))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{PaymentId}/confirm",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenConcurrencyConflict_ShouldReturnConflict()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors
                        .ConfirmationConcurrency())
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{PaymentId}/confirm",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOkAndBindReason()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var request =
            new CancelSalePaymentRequest(
                "Pembayaran dikoreksi");

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{PaymentId}/cancel",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(request, service.LastCancelRequest);
        Assert.Equal(PaymentId, service.LastPaymentId);
    }

    [Fact]
    public async Task Cancel_WhenValidationFails_ShouldReturnBadRequest()
    {
        var service = new StubService
        {
            SingleResult =
                Result<SalePaymentResponse>.Failure(
                    SalePaymentErrors.Validation(
                        "Cancellation reason is required."))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.PatchAsJsonAsync(
            $"{BasePath}/{PaymentId}/cancel",
            new CancelSalePaymentRequest(" "));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Receivable_ShouldReturnSummary()
    {
        var service = new StubService();

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/receivable");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    SaleReceivableResponse>();

        Assert.NotNull(body);
        Assert.Equal(SaleId, body!.SaleId);
        Assert.Equal(1000000m, body.SaleTotalAmount);
        Assert.Equal(250000m, body.ConfirmedPaidAmount);
        Assert.Equal(750000m, body.OutstandingReceivable);

        Assert.Equal(
            SalePaymentState.PartiallyPaid,
            body.PaymentState);

        Assert.Equal(1, service.ReceivableCallCount);
    }

    [Fact]
    public async Task Receivable_WhenSaleMissing_ShouldReturnNotFound()
    {
        var service = new StubService
        {
            ReceivableResult =
                Result<SaleReceivableResponse>.Failure(
                    SalePaymentErrors.SaleNotFound(
                        SaleId))
        };

        using var factory = new ApiFactory(service);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"{BasePath}/receivable");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static string BasePath =>
        $"/api/v1/organizations/{OrganizationId}/" +
        $"sales/{SaleId}/payments";

    private static CreateSalePaymentRequest CreateRequest()
    {
        return new CreateSalePaymentRequest(
            "PAY-001",
            PaymentDate,
            250000.13m,
            SalePaymentMethod.BankTransfer,
            "TRF-001",
            "Koperasi Tani",
            "Pembayaran pertama");
    }

    private static UpdateSalePaymentRequest UpdateRequest()
    {
        return new UpdateSalePaymentRequest(
            PaymentDate.AddDays(1),
            300000.13m,
            SalePaymentMethod.Cash,
            null,
            "Pembeli Cabang",
            "Pembayaran diperbarui");
    }

    private static SalePaymentResponse CreateResponse()
    {
        return new SalePaymentResponse(
            PaymentId,
            OrganizationId,
            SaleId,
            "PAY-001",
            PaymentDate,
            250000.13m,
            SalePaymentMethod.BankTransfer,
            "TRF-001",
            "Koperasi Tani",
            "Pembayaran pertama",
            SalePaymentStatus.Draft,
            false,
            null,
            null,
            new DateTime(
                2027,
                5,
                12,
                8,
                0,
                0,
                DateTimeKind.Utc),
            null);
    }

    private static SaleReceivableResponse
        CreateReceivableResponse()
    {
        return new SaleReceivableResponse(
            SaleId,
            "SALE-001",
            SaleDate,
            "Koperasi Tani",
            SalePaymentTerm.Credit,
            SaleDate.AddDays(30),
            1000000m,
            250000m,
            750000m,
            SalePaymentState.PartiallyPaid,
            false,
            true);
    }

    private sealed class StubService :
        ISalePaymentService
    {
        public Result<SalePaymentResponse>
            SingleResult
        { get; set; } =
                Result<SalePaymentResponse>.Success(
                    CreateResponse());

        public Result<
            IReadOnlyList<SalePaymentResponse>>
            ListResult
        { get; set; } =
                Result<
                    IReadOnlyList<SalePaymentResponse>>
                    .Success(
                        new[]
                        {
                            CreateResponse()
                        });

        public Result<SaleReceivableResponse>
            ReceivableResult
        { get; set; } =
                Result<SaleReceivableResponse>.Success(
                    CreateReceivableResponse());

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

        public Guid LastPaymentId
        {
            get;
            private set;
        }

        public CreateSalePaymentRequest? LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateSalePaymentRequest? LastUpdateRequest
        {
            get;
            private set;
        }

        public CancelSalePaymentRequest? LastCancelRequest
        {
            get;
            private set;
        }

        public SalePaymentFilter? LastFilter
        {
            get;
            private set;
        }

        public int ConfirmCallCount
        {
            get;
            private set;
        }

        public int ReceivableCallCount
        {
            get;
            private set;
        }

        public Task<Result<SalePaymentResponse>>
            CreateAsync(
                Guid organizationId,
                Guid saleId,
                CreateSalePaymentRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(organizationId, saleId);

            LastCreateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<
            Result<
                IReadOnlyList<SalePaymentResponse>>>
            GetAllAsync(
                Guid organizationId,
                Guid saleId,
                SalePaymentFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            Capture(organizationId, saleId);

            LastFilter = filter;

            return Task.FromResult(ListResult);
        }

        public Task<Result<SalePaymentResponse>>
            GetByIdAsync(
                Guid organizationId,
                Guid saleId,
                Guid paymentId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId,
                paymentId);

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SalePaymentResponse>>
            UpdateDraftAsync(
                Guid organizationId,
                Guid saleId,
                Guid paymentId,
                UpdateSalePaymentRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId,
                paymentId);

            LastUpdateRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SalePaymentResponse>>
            ConfirmAsync(
                Guid organizationId,
                Guid saleId,
                Guid paymentId,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId,
                paymentId);

            ConfirmCallCount++;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SalePaymentResponse>>
            CancelAsync(
                Guid organizationId,
                Guid saleId,
                Guid paymentId,
                CancelSalePaymentRequest request,
                CancellationToken cancellationToken = default)
        {
            Capture(
                organizationId,
                saleId,
                paymentId);

            LastCancelRequest = request;

            return Task.FromResult(SingleResult);
        }

        public Task<Result<SaleReceivableResponse>>
            GetReceivableAsync(
                Guid organizationId,
                Guid saleId,
                CancellationToken cancellationToken = default)
        {
            Capture(organizationId, saleId);

            ReceivableCallCount++;

            return Task.FromResult(ReceivableResult);
        }

        private void Capture(
            Guid organizationId,
            Guid saleId,
            Guid paymentId = default)
        {
            LastOrganizationId = organizationId;
            LastSaleId = saleId;
            LastPaymentId = paymentId;
        }
    }

    private sealed class ApiFactory :
        WebApplicationFactory<Program>
    {
        private readonly ISalePaymentService _service;

        public ApiFactory(ISalePaymentService service)
        {
            _service = service;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISalePaymentService>();
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
