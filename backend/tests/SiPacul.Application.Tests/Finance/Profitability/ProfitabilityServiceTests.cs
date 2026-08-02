using SiPacul.Application.Finance.Profitability;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Application.Finance.Profitability.Services;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using Xunit;

namespace SiPacul.Application.Tests.Finance.Profitability;

public sealed class ProfitabilityServiceTests
{
    private static readonly Organization ExistingOrganization =
        Organization.Create(
            "ORG-001",
            "Organisasi Pertanian");

    private static readonly Guid OrganizationId =
        ExistingOrganization.Id;
    private static readonly Guid CropCycleId =
            Guid.Parse(
                "20000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset GeneratedAt =
        new(
            2027,
            7,
            1,
            8,
            30,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task Get_WhenValid_ShouldReturnCalculatedReport()
    {
        var service =
            CreateService(
                Snapshot(
                    recognizedRevenue: 2000000,
                    collectedRevenue: 1500000,
                    activityResourceCost: 600000,
                    manualExpenseCost: 400000,
                    investorCapital: 700000,
                    partnerCapital: 300000,
                    availableHarvest: 125.5m));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000000m, result.Value.RecognizedRevenue);
        Assert.Equal(1500000m, result.Value.CollectedRevenue);
        Assert.Equal(500000m, result.Value.OutstandingReceivable);
        Assert.Equal(1000000m, result.Value.TotalCultivationCost);
        Assert.Equal(1000000m, result.Value.NetProfit);
        Assert.Equal(50m, result.Value.ProfitMarginPercentage);

        Assert.Equal(
            ProfitabilityOutcome.Profit,
            result.Value.Outcome);

        Assert.Equal(
            HarvestQuantityUnit.Kilogram,
            result.Value.HarvestQuantityUnit);

        Assert.Equal(
            125.5m,
            result.Value.AvailableHarvestQuantity);
    }

    [Fact]
    public async Task Get_ShouldExposeSourceSnapshots()
    {
        var service =
            CreateService(Snapshot());

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsSuccess);
        Assert.Equal("CC-001", result.Value.CropCycleCode);
        Assert.Equal("Musim Padi", result.Value.CropCycleName);
        Assert.Equal(CommodityId, result.Value.CommodityIdSnapshot);
        Assert.Equal("PADI", result.Value.CommodityCodeSnapshot);
        Assert.Equal("Padi", result.Value.CommodityNameSnapshot);
    }

    [Fact]
    public async Task Get_ShouldUseInjectedUtcTime()
    {
        var service =
            CreateService(Snapshot());

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            GeneratedAt.UtcDateTime,
            result.Value.GeneratedAt);

        Assert.Equal(
            DateTimeKind.Utc,
            result.Value.GeneratedAt.Kind);
    }

    [Fact]
    public async Task Get_ZeroRevenue_ShouldReturnNullMargin()
    {
        var service =
            CreateService(
                Snapshot(
                    recognizedRevenue: 0,
                    collectedRevenue: 0,
                    activityResourceCost: 100,
                    manualExpenseCost: 0));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ProfitMarginPercentage);
        Assert.Equal(-100m, result.Value.NetProfit);

        Assert.Equal(
            ProfitabilityOutcome.Loss,
            result.Value.Outcome);
    }

    [Fact]
    public async Task Get_EmptyOrganizationId_ShouldFailEarly()
    {
        var readRepository =
            new FakeProfitabilityReadRepository(
                Snapshot());

        var organizationRepository =
            new FakeOrganizationRepository(
                ExistingOrganization);

        var service =
            CreateService(
                readRepository,
                organizationRepository);

        var result =
            await service.GetCropCycleReportAsync(
                Guid.Empty,
                CropCycleId);

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors.ValidationCode,
            result.Error.Code);

        Assert.Equal(
            0,
            organizationRepository.GetByIdCallCount);

        Assert.Equal(
            0,
            readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_EmptyCropCycleId_ShouldFailEarly()
    {
        var readRepository =
            new FakeProfitabilityReadRepository(
                Snapshot());

        var service =
            CreateService(
                readRepository,
                new FakeOrganizationRepository(
                    ExistingOrganization));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                Guid.Empty);

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors.ValidationCode,
            result.Error.Code);

        Assert.Equal(
            0,
            readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_OrganizationMissing_ShouldReturnNotFound()
    {
        var readRepository =
            new FakeProfitabilityReadRepository(
                Snapshot());

        var service =
            CreateService(
                readRepository,
                new FakeOrganizationRepository());

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors
                .OrganizationNotFoundCode,
            result.Error.Code);

        Assert.Equal(
            0,
            readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_CropCycleMissing_ShouldReturnNotFound()
    {
        var service =
            CreateService(
                new FakeProfitabilityReadRepository(null),
                new FakeOrganizationRepository(
                    ExistingOrganization));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors
                .CropCycleNotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_RepositoryInconsistency_ShouldReturnConflict()
    {
        var readRepository =
            new FakeProfitabilityReadRepository(
                Snapshot())
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "Mixed harvest units.")
            };

        var service =
            CreateService(
                readRepository,
                new FakeOrganizationRepository(
                    ExistingOrganization));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors.SourceDataInvalidCode,
            result.Error.Code);

        Assert.Contains(
            "Mixed harvest units.",
            result.Error.Message);
    }

    [Fact]
    public async Task Get_InvalidDomainInput_ShouldReturnConflict()
    {
        var service =
            CreateService(
                Snapshot(
                    recognizedRevenue: 100,
                    collectedRevenue: 101));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId);

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProfitabilityErrors.SourceDataInvalidCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_ShouldForwardCancellationToOrganization()
    {
        using var source =
            new CancellationTokenSource();

        var organizationRepository =
            new FakeOrganizationRepository(
                ExistingOrganization);

        var service =
            CreateService(
                new FakeProfitabilityReadRepository(
                    Snapshot()),
                organizationRepository);

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId,
                source.Token);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            source.Token,
            organizationRepository.LastCancellationToken);
    }

    [Fact]
    public async Task Get_ShouldForwardCancellationToReadRepository()
    {
        using var source =
            new CancellationTokenSource();

        var readRepository =
            new FakeProfitabilityReadRepository(
                Snapshot());

        var service =
            CreateService(
                readRepository,
                new FakeOrganizationRepository(
                    ExistingOrganization));

        var result =
            await service.GetCropCycleReportAsync(
                OrganizationId,
                CropCycleId,
                source.Token);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            source.Token,
            readRepository.LastCancellationToken);
    }

    private static ProfitabilityService CreateService(
        ProfitabilitySourceSnapshot snapshot)
    {
        return CreateService(
            new FakeProfitabilityReadRepository(snapshot),
            new FakeOrganizationRepository(
                ExistingOrganization));
    }

    private static ProfitabilityService CreateService(
        IProfitabilityReadRepository readRepository,
        IOrganizationRepository organizationRepository)
    {
        return new ProfitabilityService(
            readRepository,
            organizationRepository,
            new FixedTimeProvider(GeneratedAt));
    }

    private static ProfitabilitySourceSnapshot Snapshot(
        decimal recognizedRevenue = 1000000,
        decimal collectedRevenue = 500000,
        decimal activityResourceCost = 300000,
        decimal manualExpenseCost = 200000,
        decimal investorCapital = 400000,
        decimal partnerCapital = 100000,
        decimal availableHarvest = 50)
    {
        return new ProfitabilitySourceSnapshot(
            OrganizationId,
            CropCycleId,
            "CC-001",
            "Musim Padi",
            CommodityId,
            "PADI",
            "Padi",
            recognizedRevenue,
            collectedRevenue,
            activityResourceCost,
            manualExpenseCost,
            investorCapital,
            partnerCapital,
            availableHarvest,
            HarvestQuantityUnit.Kilogram);
    }

    private sealed class
        FakeProfitabilityReadRepository :
        IProfitabilityReadRepository
    {
        private readonly ProfitabilitySourceSnapshot?
            _snapshot;

        public FakeProfitabilityReadRepository(
            ProfitabilitySourceSnapshot? snapshot)
        {
            _snapshot = snapshot;
        }

        public Exception? ExceptionToThrow
        {
            get;
            init;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
        }

        public Task<ProfitabilitySourceSnapshot?> GetAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            LastCancellationToken = cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(_snapshot);
        }
    }

    private sealed class FakeOrganizationRepository :
        IOrganizationRepository
    {
        private readonly List<Organization> _organizations;

        public FakeOrganizationRepository(
            params Organization[] organizations)
        {
            _organizations = organizations.ToList();
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
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
            GetByIdCallCount++;
            LastCancellationToken = cancellationToken;

            return Task.FromResult(
                _organizations.SingleOrDefault(
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
            _organizations.Add(organization);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
