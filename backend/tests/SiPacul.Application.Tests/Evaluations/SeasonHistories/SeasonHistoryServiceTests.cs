using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application;
using SiPacul.Application.Evaluations.SeasonHistories;
using SiPacul.Application.Evaluations.SeasonHistories.Contracts;
using SiPacul.Application.Evaluations.SeasonHistories.Persistence;
using SiPacul.Application.Evaluations.SeasonHistories.Services;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Domain.Entities.Lands;
using Xunit;

namespace SiPacul.Application.Tests.Evaluations.SeasonHistories;

public sealed class SeasonHistoryServiceTests
{
    private static readonly Guid OrganizationId = Guid.Parse(
        "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId = Guid.Parse(
        "30000000-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset GeneratedAt = new(
        2027,
        8,
        1,
        8,
        30,
        0,
        TimeSpan.Zero);

    [Fact]
    public void AddApplication_ShouldRegisterServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = services.Single(service =>
            service.ServiceType ==
                typeof(ISeasonHistoryService));

        Assert.Equal(
            typeof(SeasonHistoryService),
            descriptor.ImplementationType);
        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public void Constructor_NullReadRepository_ShouldThrow()
    {
        var land = CreateLand();

        Assert.Throws<ArgumentNullException>(() =>
            new SeasonHistoryService(
                null!,
                new FakeLandRepository(land),
                new FixedTimeProvider(GeneratedAt)));
    }

    [Fact]
    public void Constructor_NullLandRepository_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SeasonHistoryService(
                new FakeSeasonHistoryReadRepository(
                    EmptyPage()),
                null!,
                new FixedTimeProvider(GeneratedAt)));
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var land = CreateLand();

        Assert.Throws<ArgumentNullException>(() =>
            new SeasonHistoryService(
                new FakeSeasonHistoryReadRepository(
                    EmptyPage()),
                new FakeLandRepository(land),
                null!));
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("land")]
    public async Task Get_EmptyRequiredIdentifier_ShouldFailEarly(
        string identifier)
    {
        var land = CreateLand();
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage());
        var landRepository = new FakeLandRepository(land);
        var service = CreateService(
            readRepository,
            landRepository);

        var result = await service.GetAsync(
            identifier == "organization"
                ? Guid.Empty
                : OrganizationId,
            identifier == "land"
                ? Guid.Empty
                : land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.ValidationCode,
            result.Error.Code);
        Assert.Equal(0, landRepository.GetByIdCallCount);
        Assert.Equal(0, readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_EmptyPlotIdentifier_ShouldFailEarly()
    {
        var land = CreateLand();
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage());
        var service = CreateService(
            readRepository,
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id,
            new SeasonHistoryFilter(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.ValidationCode,
            result.Error.Code);
        Assert.Equal(0, readRepository.GetCallCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task Get_InvalidPagination_ShouldFailEarly(
        int page,
        int pageSize)
    {
        var land = CreateLand();
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage());
        var service = CreateService(
            readRepository,
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id,
            new SeasonHistoryFilter(
                Page: page,
                PageSize: pageSize));

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.ValidationCode,
            result.Error.Code);
        Assert.Equal(0, readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_MissingLand_ShouldReturnNotFound()
    {
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage());
        var service = CreateService(
            readRepository,
            new FakeLandRepository());

        var result = await service.GetAsync(
            OrganizationId,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.LandNotFoundCode,
            result.Error.Code);
        Assert.Equal(0, readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_MissingPlot_ShouldReturnNotFound()
    {
        var land = CreateLand();
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage());
        var service = CreateService(
            readRepository,
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id,
            new SeasonHistoryFilter(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.LandPlotNotFoundCode,
            result.Error.Code);
        Assert.Equal(0, readRepository.GetCallCount);
    }

    [Fact]
    public async Task Get_DefaultFilter_ShouldMapEvaluationPage()
    {
        var land = CreateLand();
        var plot = land.Plots.First();
        var source = CycleSource(
            land.Id,
            plot.Id,
            recognizedRevenue: 200,
            collectedRevenue: 150,
            totalCost: 100);

        var readRepository =
            new FakeSeasonHistoryReadRepository(
                new SeasonHistoryPageSource(
                    21,
                    [source]));

        var service = CreateService(
            readRepository,
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(land.Id, result.Value.LandId);
        Assert.Equal("LAND-001", result.Value.LandCode);
        Assert.Null(result.Value.LandPlotId);
        Assert.False(result.Value.IncludeNonTerminal);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Equal(21, result.Value.TotalSeasonCount);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.False(result.Value.HasPreviousPage);
        Assert.True(result.Value.HasNextPage);

        var season = Assert.Single(result.Value.Seasons);
        Assert.Equal(plot.Id, season.LandPlotId);
        Assert.Equal("PLOT-001", season.LandPlotCode);
        Assert.Equal(100m, season.NetProfit);
        Assert.Equal(50m, season.OutstandingReceivable);
        Assert.Equal(
            GeneratedAt.UtcDateTime,
            season.GeneratedAt);

        Assert.Contains(
            season.Attentions,
            attention =>
                attention.Code ==
                    SeasonEvaluationAttentionCode
                        .OutstandingReceivable);

        Assert.Equal(0, readRepository.LastSkip);
        Assert.Equal(20, readRepository.LastTake);
        Assert.False(
            readRepository.LastIncludeNonTerminal);
    }

    [Fact]
    public async Task Get_PlotFilterAndSecondPage_ShouldBeForwarded()
    {
        var land = CreateLand();
        var plot = land.Plots.Last();
        var readRepository =
            new FakeSeasonHistoryReadRepository(
                new SeasonHistoryPageSource(
                    2,
                    [CycleSource(land.Id, plot.Id)]));

        var service = CreateService(
            readRepository,
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id,
            new SeasonHistoryFilter(
                plot.Id,
                IncludeNonTerminal: true,
                Page: 2,
                PageSize: 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(plot.Id, result.Value.LandPlotId);
        Assert.Equal("PLOT-002", result.Value.LandPlotCode);
        Assert.True(result.Value.IncludeNonTerminal);
        Assert.Equal(1, readRepository.LastSkip);
        Assert.Equal(1, readRepository.LastTake);
        Assert.True(readRepository.LastIncludeNonTerminal);
        Assert.True(result.Value.HasPreviousPage);
        Assert.False(result.Value.HasNextPage);
    }

    [Fact]
    public async Task Get_EmptyPage_ShouldExposeZeroTotals()
    {
        var land = CreateLand();
        var service = CreateService(
            new FakeSeasonHistoryReadRepository(EmptyPage()),
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Seasons);
        Assert.Equal(0, result.Value.TotalSeasonCount);
        Assert.Equal(0, result.Value.TotalPages);
        Assert.False(result.Value.HasNextPage);
    }

    [Fact]
    public async Task Get_CrossTenantSource_ShouldReturnSourceFailure()
    {
        var land = CreateLand();
        var plot = land.Plots.First();
        var invalid = CycleSource(
            land.Id,
            plot.Id) with
        {
            OrganizationId = Guid.NewGuid()
        };

        var service = CreateService(
            new FakeSeasonHistoryReadRepository(
                new SeasonHistoryPageSource(1, [invalid])),
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.SourceDataInvalidCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_MissingSourcePlot_ShouldReturnSourceFailure()
    {
        var land = CreateLand();
        var invalid = CycleSource(
            land.Id,
            Guid.NewGuid());

        var service = CreateService(
            new FakeSeasonHistoryReadRepository(
                new SeasonHistoryPageSource(1, [invalid])),
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.SourceDataInvalidCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_DuplicateCycleSource_ShouldReturnSourceFailure()
    {
        var land = CreateLand();
        var plot = land.Plots.First();
        var duplicate = CycleSource(land.Id, plot.Id);

        var service = CreateService(
            new FakeSeasonHistoryReadRepository(
                new SeasonHistoryPageSource(
                    2,
                    [duplicate, duplicate])),
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.SourceDataInvalidCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_InvalidFinancialSource_ShouldReturnSourceFailure()
    {
        var land = CreateLand();
        var plot = land.Plots.First();
        var invalid = CycleSource(
            land.Id,
            plot.Id) with
        {
            RecognizedRevenue = 100,
            CollectedRevenue = 101
        };

        var service = CreateService(
            new FakeSeasonHistoryReadRepository(
                new SeasonHistoryPageSource(1, [invalid])),
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.SourceDataInvalidCode,
            result.Error.Code);
    }

    [Fact]
    public async Task Get_RepositoryException_ShouldReturnSourceFailure()
    {
        var land = CreateLand();
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage())
            {
                ExceptionToThrow =
                    new InvalidOperationException("broken source")
            };

        var service = CreateService(
            readRepository,
            new FakeLandRepository(land));

        var result = await service.GetAsync(
            OrganizationId,
            land.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            SeasonHistoryErrors.SourceDataInvalidCode,
            result.Error.Code);
        Assert.Contains("broken source", result.Error.Message);
    }

    [Fact]
    public async Task Get_ShouldForwardCancellationToken()
    {
        var land = CreateLand();
        var readRepository =
            new FakeSeasonHistoryReadRepository(EmptyPage());
        var landRepository = new FakeLandRepository(land);
        var service = CreateService(
            readRepository,
            landRepository);
        using var source = new CancellationTokenSource();

        var result = await service.GetAsync(
            OrganizationId,
            land.Id,
            cancellationToken: source.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            source.Token,
            landRepository.LastCancellationToken);
        Assert.Equal(
            source.Token,
            readRepository.LastCancellationToken);
    }

    private static SeasonHistoryService CreateService(
        ISeasonHistoryReadRepository readRepository,
        ILandRepository landRepository)
    {
        return new SeasonHistoryService(
            readRepository,
            landRepository,
            new FixedTimeProvider(GeneratedAt));
    }

    private static Land CreateLand()
    {
        var land = Land.Create(
            OrganizationId,
            "LAND-001",
            "Lahan Utama",
            LandTenureType.Owned,
            2,
            AreaUnit.Hectare,
            null,
            null,
            null,
            null,
            null);

        land.AddPlot(
            "PLOT-001",
            "Petak Satu",
            1,
            AreaUnit.Hectare,
            null,
            null);

        land.AddPlot(
            "PLOT-002",
            "Petak Dua",
            1,
            AreaUnit.Hectare,
            null,
            null);

        return land;
    }

    private static SeasonHistoryPageSource EmptyPage()
    {
        return new SeasonHistoryPageSource(
            0,
            Array.Empty<SeasonHistoryCycleSource>());
    }

    private static SeasonHistoryCycleSource CycleSource(
        Guid landId,
        Guid landPlotId,
        decimal recognizedRevenue = 200,
        decimal collectedRevenue = 200,
        decimal totalCost = 100)
    {
        return new SeasonHistoryCycleSource(
            OrganizationId,
            Guid.NewGuid(),
            "CC-001",
            "Musim Padi",
            landId,
            landPlotId,
            CommodityId,
            "PADI",
            "Padi",
            CropCycleStatus.Completed,
            new DateOnly(2027, 2, 1),
            new DateOnly(2027, 5, 1),
            new DateOnly(2027, 2, 1),
            new DateOnly(2027, 5, 1),
            2,
            2,
            0,
            0,
            0,
            1,
            1,
            0,
            0,
            1,
            recognizedRevenue,
            collectedRevenue,
            totalCost,
            0);
    }

    private sealed class FakeSeasonHistoryReadRepository :
        ISeasonHistoryReadRepository
    {
        private readonly SeasonHistoryPageSource _page;

        public FakeSeasonHistoryReadRepository(
            SeasonHistoryPageSource page)
        {
            _page = page;
        }

        public Exception? ExceptionToThrow { get; init; }

        public int GetCallCount { get; private set; }

        public int LastSkip { get; private set; }

        public int LastTake { get; private set; }

        public bool LastIncludeNonTerminal { get; private set; }

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
        }

        public Task<SeasonHistoryPageSource> GetPageAsync(
            Guid organizationId,
            Guid landId,
            Guid? landPlotId,
            bool includeNonTerminal,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            LastSkip = skip;
            LastTake = take;
            LastIncludeNonTerminal = includeNonTerminal;
            LastCancellationToken = cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(_page);
        }
    }

    private sealed class FakeLandRepository : ILandRepository
    {
        private readonly IReadOnlyList<Land> _lands;

        public FakeLandRepository(params Land[] lands)
        {
            _lands = lands;
        }

        public int GetByIdCallCount { get; private set; }

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<Land>> GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_lands);
        }

        public Task<Land?> GetByIdAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            LastCancellationToken = cancellationToken;

            return Task.FromResult(
                _lands.SingleOrDefault(land =>
                    land.OrganizationId == organizationId &&
                    land.Id == landId));
        }

        public Task<Land?> GetByIdForUpdateAsync(
            Guid organizationId,
            Guid landId,
            CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(
                organizationId,
                landId,
                cancellationToken);
        }

        public Task<bool> CodeExistsAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Land land)
        {
            throw new NotSupportedException();
        }

        public void Remove(Land land)
        {
            throw new NotSupportedException();
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
