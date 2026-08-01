using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Application.Sales;
using SiPacul.Application.Sales.Contracts;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Application.Sales.Services;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;
using Xunit;

namespace SiPacul.Application.Tests.Sales;

public sealed class SaleServiceTests
{
    private static readonly DateOnly SaleDate =
        new(2027, 5, 10);

    [Fact]
    public async Task Create_WhenValid_ShouldSaveDraft()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("SALE-001", result.Value.Code);
        Assert.Equal(SaleStatus.Draft, result.Value.Status);
        Assert.Single(context.Repository.Sales);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_WhenOrganizationMissing_ShouldFail()
    {
        var context = CreateContext(
            includeOrganization: false);

        var result = await context.Service.CreateAsync(
            Guid.NewGuid(),
            CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.OrganizationNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenCodeExists_ShouldFail()
    {
        var context = CreateContext();
        context.Repository.Sales.Add(
            CreateSale(context.Organization.Id));

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.CodeAlreadyExistsCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Create_CreditWithoutDueDate_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            context.Organization.Id,
            CreateRequest() with
            {
                PaymentTerm = SalePaymentTerm.Credit,
                DueDate = null
            });

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_WithInvalidDateRange_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.GetAllAsync(
            context.Organization.Id,
            new SaleFilter(
                SaleDateFrom: SaleDate.AddDays(1),
                SaleDateTo: SaleDate));

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldReturnMappedSales()
    {
        var context = CreateContext();
        context.Repository.Sales.Add(
            CreateSale(context.Organization.Id));

        var result = await context.Service.GetAllAsync(
            context.Organization.Id,
            new SaleFilter(
                BuyerName: "koperasi"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("SALE-001", result.Value[0].Code);
        Assert.Equal("koperasi", context.Repository.LastBuyerName);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldFail()
    {
        var context = CreateContext();

        var result = await context.Service.GetByIdAsync(
            context.Organization.Id,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateDraft_WithValidValues_ShouldSave()
    {
        var context = CreateContextWithSale();

        var result = await context.Service.UpdateDraftAsync(
            context.Organization.Id,
            context.Sale!.Id,
            new UpdateSaleRequest(
                SaleDate.AddDays(1),
                "Pedagang Besar",
                "0899",
                "Pasar Induk",
                SalePaymentTerm.Credit,
                SaleDate.AddDays(30),
                0,
                "Kredit"));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            SalePaymentTerm.Credit,
            result.Value.PaymentTerm);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddLine_WhenValid_ShouldUseSnapshots()
    {
        var context = CreateContextWithSale();
        context.Repository.Reference =
            CreateReference();

        var result = await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            AddLineRequest());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Lines);
        Assert.Equal(
            "HRV-001",
            result.Value.Lines[0]
                .HarvestBatchCodeSnapshot);
        Assert.Equal(
            "Padi",
            result.Value.Lines[0]
                .CommodityNameSnapshot);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddLine_WhenHarvestMissing_ShouldFail()
    {
        var context = CreateContextWithSale();

        var result = await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            AddLineRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.HarvestBatchNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task AddLine_WhenHarvestDraft_ShouldFail()
    {
        var context = CreateContextWithSale();
        context.Repository.Reference =
            CreateReference(
                status: HarvestBatchStatus.Draft);

        var result = await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            AddLineRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.HarvestBatchNotConfirmedCode,
            result.Error.Code);
    }

    [Fact]
    public async Task AddLine_WhenUnitMismatch_ShouldFail()
    {
        var context = CreateContextWithSale();
        context.Repository.Reference =
            CreateReference();

        var result = await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            AddLineRequest() with
            {
                QuantityUnit = HarvestQuantityUnit.Ton
            });

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.QuantityUnitMismatchCode,
            result.Error.Code);
    }

    [Fact]
    public async Task AddLine_WhenQuantityExceedsAvailable_ShouldFail()
    {
        var context = CreateContextWithSale();
        context.Repository.Reference =
            CreateReference(netQuantity: 100);
        context.Repository.SoldQuantity = 80;

        var result = await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            AddLineRequest() with
            {
                Quantity = 21
            });

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.InsufficientQuantityCode,
            result.Error.Code);
    }

    [Fact]
    public async Task AddLine_WhenDuplicateBatch_ShouldFail()
    {
        var context = CreateContextWithSale();
        context.Repository.Reference =
            CreateReference();

        await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            AddLineRequest());

        var result = await context.Service.AddLineAsync(
            context.Organization.Id,
            context.Sale.Id,
            AddLineRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.InvalidStatusTransitionCode,
            result.Error.Code);
    }

    [Fact]
    public async Task UpdateLine_WhenValid_ShouldRecalculate()
    {
        var context = CreateContextWithLine();
        var lineId = context.Sale!.Lines.Single().Id;

        var result = await context.Service.UpdateLineAsync(
            context.Organization.Id,
            context.Sale.Id,
            lineId,
            new UpdateSaleLineRequest(
                20,
                3000,
                1000,
                "Updated"));

        Assert.True(result.IsSuccess);
        Assert.Equal(59000m, result.Value.Subtotal);
        Assert.Equal(20m, result.Value.Lines[0].Quantity);
    }

    [Fact]
    public async Task UpdateLine_WhenMissing_ShouldFail()
    {
        var context = CreateContextWithLine();

        var result = await context.Service.UpdateLineAsync(
            context.Organization.Id,
            context.Sale!.Id,
            Guid.NewGuid(),
            new UpdateSaleLineRequest(
                1,
                100,
                0,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.LineNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task RemoveLine_WhenValid_ShouldSave()
    {
        var context = CreateContextWithLine();
        var lineId = context.Sale!.Lines.Single().Id;

        var result = await context.Service.RemoveLineAsync(
            context.Organization.Id,
            context.Sale.Id,
            lineId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Lines);
        Assert.Equal(0m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Confirm_WhenProcessorSucceeds_ShouldReturnConfirmed()
    {
        var context = CreateContextWithLine();
        context.Sale!.Confirm();
        context.Processor.Result =
            SaleConfirmationResult.Succeeded(
                context.Sale);

        var result = await context.Service.ConfirmAsync(
            context.Organization.Id,
            context.Sale.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            SaleStatus.Confirmed,
            result.Value.Status);
    }

    [Fact]
    public async Task Confirm_WhenInsufficient_ShouldMapConflict()
    {
        var context = CreateContextWithSale();
        var harvestBatchId = Guid.NewGuid();

        context.Processor.Result =
            SaleConfirmationResult.Failed(
                SaleConfirmationFailure
                    .InsufficientQuantity,
                harvestBatchId,
                50,
                40);

        var result = await context.Service.ConfirmAsync(
            context.Organization.Id,
            context.Sale!.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.InsufficientQuantityCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Confirm_WhenConcurrencyFails_ShouldMapConflict()
    {
        var context = CreateContextWithSale();
        context.Processor.Result =
            SaleConfirmationResult.Failed(
                SaleConfirmationFailure
                    .ConcurrencyConflict);

        var result = await context.Service.ConfirmAsync(
            context.Organization.Id,
            context.Sale!.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.ConfirmationConcurrencyCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromDraft_ShouldSave()
    {
        var context = CreateContextWithSale();

        var result = await context.Service.CancelAsync(
            context.Organization.Id,
            context.Sale!.Id,
            new CancelSaleRequest("Batal"));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            SaleStatus.Cancelled,
            result.Value.Status);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cancel_WithEmptyReason_ShouldFail()
    {
        var context = CreateContextWithSale();

        var result = await context.Service.CancelAsync(
            context.Organization.Id,
            context.Sale!.Id,
            new CancelSaleRequest(" "));

        Assert.True(result.IsFailure);
        Assert.Equal(
            SaleErrors.ValidationCode,
            result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ShouldPassFiltersToRepository()
    {
        var context = CreateContext();
        var filter = new SaleFilter(
            SaleStatus.Confirmed,
            SaleDate,
            SaleDate.AddDays(5),
            SalePaymentTerm.Credit,
            "  Pasar  ");

        var result = await context.Service.GetAllAsync(
            context.Organization.Id,
            filter);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            SaleStatus.Confirmed,
            context.Repository.LastStatus);
        Assert.Equal(
            SalePaymentTerm.Credit,
            context.Repository.LastPaymentTerm);
        Assert.Equal("Pasar", context.Repository.LastBuyerName);
    }

    private static TestContext CreateContext(
        bool includeOrganization = true)
    {
        var organization = Organization.Create(
            "ORG-001",
            "Organisasi Tani");

        var repository = new FakeSaleRepository();
        var processor = new FakeConfirmationProcessor();
        var unitOfWork = new FakeUnitOfWork();

        var organizationRepository =
            includeOrganization
                ? new FakeOrganizationRepository(
                    organization)
                : new FakeOrganizationRepository();

        var service = new SaleService(
            repository,
            processor,
            organizationRepository,
            unitOfWork);

        return new TestContext(
            organization,
            null,
            service,
            repository,
            processor,
            unitOfWork);
    }

    private static TestContext CreateContextWithSale()
    {
        var context = CreateContext();
        var sale = CreateSale(
            context.Organization.Id);

        context.Repository.Sales.Add(sale);

        return context with { Sale = sale };
    }

    private static TestContext CreateContextWithLine()
    {
        var context = CreateContextWithSale();
        context.Repository.Reference =
            CreateReference();

        context.Sale!.AddLine(
            context.Repository.Reference.HarvestBatchId,
            context.Repository.Reference.HarvestBatchCode,
            context.Repository.Reference.CropCycleId,
            context.Repository.Reference.CropCycleCode,
            context.Repository.Reference.CommodityId,
            context.Repository.Reference.CommodityCode,
            context.Repository.Reference.CommodityName,
            context.Repository.Reference.QualityGrade,
            10,
            HarvestQuantityUnit.Kilogram,
            2500,
            0,
            null);

        return context;
    }

    private static Sale CreateSale(Guid organizationId)
    {
        return Sale.Create(
            organizationId,
            "SALE-001",
            SaleDate,
            "Koperasi Tani",
            null,
            null,
            SalePaymentTerm.Cash,
            null,
            0,
            null);
    }

    private static CreateSaleRequest CreateRequest()
    {
        return new CreateSaleRequest(
            "sale-001",
            SaleDate,
            "Koperasi Tani",
            null,
            null,
            SalePaymentTerm.Cash,
            null,
            null);
    }

    private static AddSaleLineRequest AddLineRequest()
    {
        return new AddSaleLineRequest(
            CreateReference().HarvestBatchId,
            10,
            HarvestQuantityUnit.Kilogram,
            2500,
            0,
            null);
    }

    private static SaleHarvestReference CreateReference(
        HarvestBatchStatus status =
            HarvestBatchStatus.Confirmed,
        decimal netQuantity = 100)
    {
        return new SaleHarvestReference(
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
            status,
            netQuantity,
            HarvestQuantityUnit.Kilogram);
    }

    private sealed record TestContext(
        Organization Organization,
        Sale? Sale,
        SaleService Service,
        FakeSaleRepository Repository,
        FakeConfirmationProcessor Processor,
        FakeUnitOfWork UnitOfWork);

    private sealed class FakeSaleRepository :
        ISaleRepository
    {
        public List<Sale> Sales { get; } = [];

        public SaleHarvestReference? Reference
        {
            get;
            set;
        }

        public decimal SoldQuantity { get; set; }

        public SaleStatus? LastStatus { get; private set; }

        public SalePaymentTerm? LastPaymentTerm
        {
            get;
            private set;
        }

        public string? LastBuyerName { get; private set; }

        public Task<IReadOnlyList<Sale>> GetAllAsync(
            Guid organizationId,
            SaleStatus? status = null,
            DateOnly? saleDateFrom = null,
            DateOnly? saleDateTo = null,
            SalePaymentTerm? paymentTerm = null,
            string? buyerName = null,
            CancellationToken cancellationToken = default)
        {
            LastStatus = status;
            LastPaymentTerm = paymentTerm;
            LastBuyerName = buyerName;

            IReadOnlyList<Sale> result = Sales
                .Where(sale =>
                    sale.OrganizationId == organizationId &&
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
            return Task.FromResult(
                Find(organizationId, saleId));
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Sales.Any(sale =>
                    sale.OrganizationId == organizationId &&
                    sale.Code == code &&
                    !sale.IsDeleted));
        }

        public Task<SaleHarvestReference?>
            GetHarvestReferenceAsync(
                Guid organizationId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            var result = Reference is not null &&
                Reference.HarvestBatchId == harvestBatchId
                    ? Reference
                    : null;

            return Task.FromResult(result);
        }

        public Task<IReadOnlyDictionary<Guid, decimal>>
            GetConfirmedSoldQuantitiesAsync(
                Guid organizationId,
                IReadOnlyCollection<Guid> harvestBatchIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, decimal> result =
                Reference is not null &&
                harvestBatchIds.Contains(
                    Reference.HarvestBatchId)
                    ? new Dictionary<Guid, decimal>
                    {
                        [Reference.HarvestBatchId] =
                            SoldQuantity
                    }
                    : new Dictionary<Guid, decimal>();

            return Task.FromResult(result);
        }

        public Task<decimal>
            GetConfirmedSoldQuantityAsync(
                Guid organizationId,
                Guid harvestBatchId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SoldQuantity);
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

    private sealed class FakeConfirmationProcessor :
        ISaleConfirmationProcessor
    {
        public SaleConfirmationResult Result
        {
            get;
            set;
        } = SaleConfirmationResult.Failed(
            SaleConfirmationFailure.SaleNotFound);

        public Task<SaleConfirmationResult> ConfirmAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeOrganizationRepository :
        IOrganizationRepository
    {
        private readonly List<Organization>
            _organizations;

        public FakeOrganizationRepository(
            params Organization[] organizations)
        {
            _organizations = organizations.ToList();
        }

        public Task<IReadOnlyList<Organization>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Organization> result =
                _organizations.ToArray();

            return Task.FromResult(result);
        }

        public Task<Organization?> GetByIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _organizations.SingleOrDefault(
                    organization =>
                        organization.Id == organizationId &&
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
            _organizations.Add(organization);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }
}
