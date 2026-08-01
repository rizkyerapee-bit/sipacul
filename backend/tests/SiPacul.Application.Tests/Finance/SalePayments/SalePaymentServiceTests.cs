using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Finance.SalePayments;
using SiPacul.Application.Finance.SalePayments.Contracts;
using SiPacul.Application.Finance.SalePayments.Persistence;
using SiPacul.Application.Finance.SalePayments.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Application.Sales.Services;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;
using Xunit;

namespace SiPacul.Application.Tests.Finance.SalePayments;

public sealed class SalePaymentServiceTests
{
    private static readonly DateOnly SaleDate =
        new(2027, 5, 10);

    private static readonly DateOnly PaymentDate =
        new(2027, 5, 12);

    [Fact]
    public async Task Create_WhenValid_ShouldCreateDraft()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("PAY-001", result.Value.Code);
        Assert.Equal(250000.13m, result.Value.Amount);

        Assert.Equal(
            SalePaymentStatus.Draft,
            result.Value.Status);

        Assert.False(result.Value.IsCollectedRevenue);
        Assert.Single(context.PaymentRepository.Payments);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenRequestNull_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            null!);

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldFail()
    {
        var context = CreateContext(
            includeOrganization: false);

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenSaleMissing_ShouldFail()
    {
        var context = CreateContext(
            includeSale: false);

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.SaleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenSaleNotConfirmed_ShouldFail()
    {
        var context = CreateContext(
            confirmSale: false);

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.SaleNotConfirmedCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenPaymentDateBeforeSale_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest() with
            {
                PaymentDate = SaleDate.AddDays(-1)
            });

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors
                .PaymentDateBeforeSaleDateCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldFail()
    {
        var context = CreateContext();

        context.PaymentRepository.Payments.Add(
            CreatePayment(
                context,
                "PAY-001"));

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.CodeAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenDomainValidationFails_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            context.Sale.Id,
            CreateRequest() with
            {
                Amount = 0
            });

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldNormalizeAndPassFilters()
    {
        var context = CreateContext();

        var payment = CreatePayment(context);

        payment.Confirm();

        context.PaymentRepository.Payments.Add(payment);

        var result = await context.Service.GetAllAsync(
            context.Organization.Id,
            context.Sale.Id,
            new SalePaymentFilter(
                SalePaymentStatus.Confirmed,
                SalePaymentMethod.BankTransfer,
                new DateOnly(2027, 5, 1),
                new DateOnly(2027, 5, 31),
                "  koperasi  "));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        Assert.Equal(
            SalePaymentStatus.Confirmed,
            context.PaymentRepository.LastStatus);

        Assert.Equal(
            SalePaymentMethod.BankTransfer,
            context.PaymentRepository.LastMethod);

        Assert.Equal(
            "koperasi",
            context.PaymentRepository.LastReceivedFrom);
    }

    [Fact]
    public async Task GetAll_WithInvalidFilter_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.GetAllAsync(
            context.Organization.Id,
            context.Sale.Id,
            new SalePaymentFilter(
                Status: (SalePaymentStatus)999));

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithInvalidDateRange_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.GetAllAsync(
            context.Organization.Id,
            context.Sale.Id,
            new SalePaymentFilter(
                PaymentDateFrom:
                    new DateOnly(2027, 6, 1),
                PaymentDateTo:
                    new DateOnly(2027, 5, 1)));

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnPayment()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        context.PaymentRepository.Payments.Add(payment);

        var result = await context.Service.GetByIdAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(payment.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.GetByIdAsync(
            context.Organization.Id,
            context.Sale.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WhenValid_ShouldUpdate()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        context.PaymentRepository.Payments.Add(payment);

        var result = await context.Service.UpdateDraftAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id,
            UpdateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(300000.13m, result.Value.Amount);
        Assert.Equal("Pembeli Cabang", result.Value.ReceivedFrom);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WithSameValues_ShouldNotSave()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        context.PaymentRepository.Payments.Add(payment);

        var result = await context.Service.UpdateDraftAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id,
            new UpdateSalePaymentRequest(
                PaymentDate,
                250000.125m,
                SalePaymentMethod.BankTransfer,
                "TRF-001",
                "Koperasi Tani",
                "Pembayaran pertama"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateDraft_WhenConfirmed_ShouldFail()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        payment.Confirm();

        context.PaymentRepository.Payments.Add(payment);

        var result = await context.Service.UpdateDraftAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id,
            UpdateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors
                .InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Confirm_WhenProcessorSucceeds_ShouldReturnConfirmed()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        payment.Confirm();

        context.Processor.Result =
            SalePaymentConfirmationResult.Succeeded(
                payment,
                payment.Amount,
                context.Sale.TotalAmount);

        var result = await context.Service.ConfirmAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            SalePaymentStatus.Confirmed,
            result.Value.Status);

        Assert.True(result.Value.IsCollectedRevenue);
    }

    [Fact]
    public async Task Confirm_WhenOverpayment_ShouldFail()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        context.Processor.Result =
            SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure.Overpayment,
                confirmedPaidAmount: 1000000.01m,
                saleTotalAmount: 1000000m);

        var result = await context.Service.ConfirmAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.OverpaymentCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Confirm_WhenConcurrencyConflict_ShouldFail()
    {
        var context = CreateContext();

        context.Processor.Result =
            SalePaymentConfirmationResult.Failed(
                SalePaymentConfirmationFailure
                    .ConcurrencyConflict);

        var result = await context.Service.ConfirmAsync(
            context.Organization.Id,
            context.Sale.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors
                .ConfirmationConcurrencyCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromConfirmed_ShouldStopCollectedRevenue()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        payment.Confirm();

        context.PaymentRepository.Payments.Add(payment);

        var confirmedAt = payment.ConfirmedAt;

        var result = await context.Service.CancelAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id,
            new CancelSalePaymentRequest(
                "  Pembayaran dikoreksi  "));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            SalePaymentStatus.Cancelled,
            result.Value.Status);

        Assert.False(result.Value.IsCollectedRevenue);

        Assert.Equal(
            confirmedAt,
            result.Value.ConfirmedAt);

        Assert.Equal(
            "Pembayaran dikoreksi",
            result.Value.CancellationReason);
    }

    [Fact]
    public async Task Cancel_WithBlankReason_ShouldFail()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        context.PaymentRepository.Payments.Add(payment);

        var result = await context.Service.CancelAsync(
            context.Organization.Id,
            context.Sale.Id,
            payment.Id,
            new CancelSalePaymentRequest(" "));

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Receivable_WithPartialPayment_ShouldBePartial()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        payment.Confirm();

        context.PaymentRepository.Payments.Add(payment);

        var result =
            await context.Service.GetReceivableAsync(
                context.Organization.Id,
                context.Sale.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000000m, result.Value.SaleTotalAmount);
        Assert.Equal(250000.13m, result.Value.ConfirmedPaidAmount);
        Assert.Equal(749999.87m, result.Value.OutstandingReceivable);

        Assert.Equal(
            SalePaymentState.PartiallyPaid,
            result.Value.PaymentState);
    }

    [Fact]
    public async Task Receivable_WithFullPayment_ShouldBePaid()
    {
        var context = CreateContext();

        var payment = SalePayment.Create(
            context.Organization.Id,
            context.Sale.Id,
            "PAY-FULL",
            PaymentDate,
            context.Sale.TotalAmount,
            SalePaymentMethod.Cash,
            null,
            null,
            null);

        payment.Confirm();

        context.PaymentRepository.Payments.Add(payment);

        var result =
            await context.Service.GetReceivableAsync(
                context.Organization.Id,
                context.Sale.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsFullyPaid);
        Assert.Equal(0m, result.Value.OutstandingReceivable);
        Assert.Equal(SalePaymentState.Paid, result.Value.PaymentState);
    }

    [Fact]
    public async Task CrossOrganizationSale_ShouldFail()
    {
        var context = CreateContext();

        var otherOrganization =
            Organization.Create(
                "ORG-002",
                "Organisasi Lain");

        context.OrganizationRepository.Organizations.Add(
            otherOrganization);

        var result = await context.Service.CreateAsync(
            otherOrganization.Id,
            context.Sale.Id,
            CreateRequest());

        Assert.True(result.IsFailure);

        Assert.Equal(
            SalePaymentErrors.SaleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task EmptyIdentifiers_ShouldFail()
    {
        var context = CreateContext();

        var emptyOrganization =
            await context.Service.GetAllAsync(
                Guid.Empty,
                context.Sale.Id);

        var emptySale =
            await context.Service.GetAllAsync(
                context.Organization.Id,
                Guid.Empty);

        var emptyPayment =
            await context.Service.GetByIdAsync(
                context.Organization.Id,
                context.Sale.Id,
                Guid.Empty);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            emptyOrganization.Error.Code);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            emptySale.Error.Code);

        Assert.Equal(
            SalePaymentErrors.ValidationCode,
            emptyPayment.Error.Code);
    }

    [Fact]
    public async Task SaleCancel_WithConfirmedPayment_ShouldFail()
    {
        var context = CreateContext();
        var payment = CreatePayment(context);

        payment.Confirm();

        context.PaymentRepository.Payments.Add(payment);

        var saleService = new SaleService(
            context.SaleRepository,
            new FakeSaleConfirmationProcessor(),
            context.OrganizationRepository,
            context.UnitOfWork,
            context.PaymentRepository);

        var result = await saleService.CancelAsync(
            context.Organization.Id,
            context.Sale.Id,
            new Application.Sales.Contracts.CancelSaleRequest(
                "Pembeli membatalkan"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            Application.Sales.SaleErrors
                .ConfirmedPaymentsExistCode,
            result.Error.Code);

        Assert.Equal(
            SaleStatus.Confirmed,
            context.Sale.Status);
    }

    private static TestContext CreateContext(
        bool includeOrganization = true,
        bool includeSale = true,
        bool confirmSale = true)
    {
        var organization =
            Organization.Create(
                "ORG-001",
                "Organisasi Pertanian");

        var sale = CreateSale(
            organization.Id,
            confirmSale);

        var paymentRepository =
            new FakeSalePaymentRepository();

        var saleRepository =
            new FakeSaleRepository(
                includeSale
                    ? new[] { sale }
                    : Array.Empty<Sale>());

        var processor =
            new FakeSalePaymentConfirmationProcessor();

        var organizationRepository =
            new FakeOrganizationRepository(
                includeOrganization
                    ? new[] { organization }
                    : Array.Empty<Organization>());

        var unitOfWork = new FakeUnitOfWork();

        var service = new SalePaymentService(
            paymentRepository,
            saleRepository,
            processor,
            organizationRepository,
            unitOfWork);

        return new TestContext(
            organization,
            sale,
            paymentRepository,
            saleRepository,
            processor,
            organizationRepository,
            unitOfWork,
            service);
    }

    private static Sale CreateSale(
        Guid organizationId,
        bool confirm)
    {
        var sale = Sale.Create(
            organizationId,
            "SALE-001",
            SaleDate,
            "Koperasi Tani",
            null,
            null,
            SalePaymentTerm.Credit,
            SaleDate.AddDays(30),
            0,
            null);

        sale.AddLine(
            Guid.Parse(
                "20000000-0000-0000-0000-000000000001"),
            "HRV-001",
            Guid.Parse(
                "30000000-0000-0000-0000-000000000001"),
            "CC-001",
            Guid.Parse(
                "40000000-0000-0000-0000-000000000001"),
            "PADI",
            "Padi",
            "Grade A",
            10,
            HarvestQuantityUnit.Kilogram,
            100000,
            0,
            null);

        if (confirm)
        {
            sale.Confirm();
        }

        return sale;
    }

    private static SalePayment CreatePayment(
        TestContext context,
        string code = "PAY-001")
    {
        return SalePayment.Create(
            context.Organization.Id,
            context.Sale.Id,
            code,
            PaymentDate,
            250000.125m,
            SalePaymentMethod.BankTransfer,
            "TRF-001",
            "Koperasi Tani",
            "Pembayaran pertama");
    }

    private static CreateSalePaymentRequest CreateRequest()
    {
        return new CreateSalePaymentRequest(
            "  pay-001  ",
            PaymentDate,
            250000.125m,
            SalePaymentMethod.BankTransfer,
            "  TRF-001  ",
            "  Koperasi Tani  ",
            "  Pembayaran pertama  ");
    }

    private static UpdateSalePaymentRequest UpdateRequest()
    {
        return new UpdateSalePaymentRequest(
            PaymentDate.AddDays(1),
            300000.125m,
            SalePaymentMethod.Cash,
            null,
            "  Pembeli Cabang  ",
            "  Pembayaran diperbarui  ");
    }

    private sealed record TestContext(
        Organization Organization,
        Sale Sale,
        FakeSalePaymentRepository PaymentRepository,
        FakeSaleRepository SaleRepository,
        FakeSalePaymentConfirmationProcessor Processor,
        FakeOrganizationRepository OrganizationRepository,
        FakeUnitOfWork UnitOfWork,
        SalePaymentService Service);

    private sealed class FakeSalePaymentRepository :
        ISalePaymentRepository
    {
        public List<SalePayment> Payments { get; } = [];

        public SalePaymentStatus? LastStatus
        {
            get;
            private set;
        }

        public SalePaymentMethod? LastMethod
        {
            get;
            private set;
        }

        public string? LastReceivedFrom
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<SalePayment>> GetAllAsync(
            Guid organizationId,
            Guid saleId,
            SalePaymentStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            DateOnly? paymentDateFrom = null,
            DateOnly? paymentDateTo = null,
            string? receivedFrom = null,
            CancellationToken cancellationToken = default)
        {
            LastStatus = status;
            LastMethod = paymentMethod;
            LastReceivedFrom = receivedFrom;

            IEnumerable<SalePayment> query =
                Payments.Where(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    !payment.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(payment =>
                    payment.Status == status.Value);
            }

            if (paymentMethod.HasValue)
            {
                query = query.Where(payment =>
                    payment.PaymentMethod ==
                        paymentMethod.Value);
            }

            if (paymentDateFrom.HasValue)
            {
                query = query.Where(payment =>
                    payment.PaymentDate >=
                        paymentDateFrom.Value);
            }

            if (paymentDateTo.HasValue)
            {
                query = query.Where(payment =>
                    payment.PaymentDate <=
                        paymentDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(receivedFrom))
            {
                query = query.Where(payment =>
                    payment.ReceivedFrom is not null &&
                    payment.ReceivedFrom.Contains(
                        receivedFrom,
                        StringComparison.OrdinalIgnoreCase));
            }

            IReadOnlyList<SalePayment> result =
                query
                    .OrderBy(payment => payment.PaymentDate)
                    .ThenBy(payment => payment.Code)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<SalePayment?> GetByIdAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(
                    organizationId,
                    saleId,
                    paymentId));
        }

        public Task<SalePayment?>
            GetByIdForUpdateAsync(
                Guid organizationId,
                Guid saleId,
                Guid paymentId,
                CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                saleId,
                paymentId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Payments.Any(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.Code == code &&
                    !payment.IsDeleted));
        }

        public Task<decimal>
            GetConfirmedPaidAmountAsync(
                Guid organizationId,
                Guid saleId,
                Guid? excludedPaymentId = null,
                CancellationToken cancellationToken = default)
        {
            var total = Payments
                .Where(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    payment.Status ==
                        SalePaymentStatus.Confirmed &&
                    !payment.IsDeleted &&
                    (
                        !excludedPaymentId.HasValue ||
                        payment.Id !=
                            excludedPaymentId.Value
                    ))
                .Sum(payment => payment.Amount);

            return Task.FromResult(
                Math.Round(
                    total,
                    2,
                    MidpointRounding.AwayFromZero));
        }

        public Task<bool> HasConfirmedPaymentsAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Payments.Any(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    payment.SaleId == saleId &&
                    payment.Status ==
                        SalePaymentStatus.Confirmed &&
                    !payment.IsDeleted));
        }

        public void Add(SalePayment payment)
        {
            Payments.Add(payment);
        }

        private SalePayment? Find(
            Guid organizationId,
            Guid saleId,
            Guid paymentId)
        {
            return Payments.SingleOrDefault(payment =>
                payment.OrganizationId == organizationId &&
                payment.SaleId == saleId &&
                payment.Id == paymentId &&
                !payment.IsDeleted);
        }
    }

    private sealed class FakeSaleRepository :
        ISaleRepository
    {
        public FakeSaleRepository(
            params Sale[] sales)
        {
            Sales = sales.ToList();
        }

        public List<Sale> Sales { get; }

        public Task<IReadOnlyList<Sale>> GetAllAsync(
            Guid organizationId,
            SaleStatus? status = null,
            DateOnly? saleDateFrom = null,
            DateOnly? saleDateTo = null,
            SalePaymentTerm? paymentTerm = null,
            string? buyerName = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Sale> result =
                Sales.Where(sale =>
                        sale.OrganizationId ==
                            organizationId &&
                        !sale.IsDeleted)
                    .ToArray();

            return Task.FromResult(result);
        }

        public Task<Sale?> GetByIdAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Find(organizationId, saleId));
        }

        public Task<Sale?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                saleId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<SaleHarvestReference?>
            GetHarvestReferenceAsync(
                Guid organizationId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                SaleHarvestReference?>(null);
        }

        public Task<IReadOnlyDictionary<Guid, decimal>>
            GetConfirmedSoldQuantitiesAsync(
                Guid organizationId,
                IReadOnlyCollection<Guid> harvestBatchIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, decimal> result =
                new Dictionary<Guid, decimal>();

            return Task.FromResult(result);
        }

        public Task<decimal>
            GetConfirmedSoldQuantityAsync(
                Guid organizationId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0m);
        }

        public Task<bool>
            HasActiveConfirmedSaleForHarvestAsync(
                Guid organizationId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Sale sale)
        {
            Sales.Add(sale);
        }

        private Sale? Find(
            Guid organizationId,
            Guid saleId)
        {
            return Sales.SingleOrDefault(sale =>
                sale.OrganizationId == organizationId &&
                sale.Id == saleId &&
                !sale.IsDeleted);
        }
    }

    private sealed class FakeSalePaymentConfirmationProcessor :
        ISalePaymentConfirmationProcessor
    {
        public SalePaymentConfirmationResult Result
        {
            get;
            set;
        } = SalePaymentConfirmationResult.Failed(
            SalePaymentConfirmationFailure
                .PaymentNotFound);

        public Task<SalePaymentConfirmationResult>
            ConfirmAsync(
                Guid organizationId,
                Guid saleId,
                Guid paymentId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeSaleConfirmationProcessor :
        ISaleConfirmationProcessor
    {
        public Task<Application.Sales.Persistence.SaleConfirmationResult>
            ConfirmAsync(
                Guid organizationId,
                Guid saleId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Application.Sales.Persistence
                    .SaleConfirmationResult.Failed(
                        Application.Sales.Persistence
                            .SaleConfirmationFailure
                            .SaleNotFound));
        }
    }

    private sealed class FakeOrganizationRepository :
        IOrganizationRepository
    {
        public FakeOrganizationRepository(
            params Organization[] organizations)
        {
            Organizations = organizations.ToList();
        }

        public List<Organization> Organizations { get; }

        public Task<IReadOnlyList<Organization>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Organization> result =
                Organizations.ToArray();

            return Task.FromResult(result);
        }

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Organizations.SingleOrDefault(
                    organization =>
                        organization.Id ==
                            organizationId &&
                        !organization.IsDeleted));
        }

        public Task<Organization?> GetByIdForUpdateAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Organization organization)
        {
            Organizations.Add(organization);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount
        {
            get;
            private set;
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }
}
