using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Evaluations.SeasonHistories.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Data.Repositories;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Repositories;

public sealed class SeasonHistoryReadRepositoryTests
{
    private static readonly Guid OrganizationId = Guid.Parse(
        "10000000-0000-0000-0000-000000000001");

    private static readonly Guid LandId = Guid.Parse(
        "20000000-0000-0000-0000-000000000001");

    [Fact]
    public void Repository_ShouldImplementContract()
    {
        using var context = CreateContext();

        Assert.IsAssignableFrom<ISeasonHistoryReadRepository>(
            new SeasonHistoryReadRepository(context));
    }

    [Fact]
    public void Repository_ShouldBeSealed()
    {
        Assert.True(
            typeof(SeasonHistoryReadRepository).IsSealed);
    }

    [Fact]
    public void Constructor_NullContext_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SeasonHistoryReadRepository(null!));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterRepositoryAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;" +
                        "Port=5432;" +
                        "Database=sipacul_tests;" +
                        "Username=sipacul;" +
                        "Password=sipacul"
                })
            .Build();

        services.AddInfrastructure(configuration);

        var descriptor = services.Single(service =>
            service.ServiceType ==
                typeof(ISeasonHistoryReadRepository));

        Assert.Equal(
            typeof(SeasonHistoryReadRepository),
            descriptor.ImplementationType);
        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public async Task GetPage_EmptyStore_ShouldReturnEmptyPage()
    {
        await using var context = CreateContext();
        var repository =
            new SeasonHistoryReadRepository(context);

        var result = await repository.GetPageAsync(
            OrganizationId,
            LandId,
            null,
            includeNonTerminal: false,
            skip: 0,
            take: 20);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Cycles);
    }

    [Fact]
    public async Task GetPage_ShouldFilterTerminalCyclesAndPlot()
    {
        await using var context = CreateContext();
        var commodity = Commodity.Create(
            OrganizationId,
            CommodityCode.Create("PADI"),
            "Padi",
            Guid.NewGuid(),
            null,
            null);

        var terminal = CropCycle.Create(
            OrganizationId,
            "CC-001",
            "Musim Selesai",
            commodity.Id,
            null,
            LandId,
            Guid.Parse(
                "21000000-0000-0000-0000-000000000001"),
            1,
            AreaUnit.Hectare,
            new DateOnly(2027, 2, 1),
            new DateOnly(2027, 5, 1),
            null);

        terminal.Start(new DateOnly(2027, 2, 1));
        terminal.Complete(new DateOnly(2027, 5, 1));

        var planned = CropCycle.Create(
            OrganizationId,
            "CC-002",
            "Musim Rencana",
            commodity.Id,
            null,
            LandId,
            Guid.Parse(
                "21000000-0000-0000-0000-000000000002"),
            1,
            AreaUnit.Hectare,
            new DateOnly(2027, 6, 1),
            new DateOnly(2027, 9, 1),
            null);

        context.AddRange(commodity, terminal, planned);
        await context.SaveChangesAsync();

        var repository =
            new SeasonHistoryReadRepository(context);

        var terminalOnly = await repository.GetPageAsync(
            OrganizationId,
            LandId,
            null,
            includeNonTerminal: false,
            skip: 0,
            take: 20);

        Assert.Equal(1, terminalOnly.TotalCount);
        var terminalSource = Assert.Single(
            terminalOnly.Cycles);
        Assert.Equal(terminal.Id, terminalSource.CropCycleId);
        Assert.Equal("PADI", terminalSource.CommodityCode);
        Assert.Equal(0m, terminalSource.TotalCultivationCost);

        var selectedPlot = await repository.GetPageAsync(
            OrganizationId,
            LandId,
            planned.LandPlotId,
            includeNonTerminal: true,
            skip: 0,
            take: 20);

        Assert.Equal(1, selectedPlot.TotalCount);
        Assert.Equal(
            planned.Id,
            Assert.Single(selectedPlot.Cycles).CropCycleId);
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("land")]
    [InlineData("plot")]
    public async Task GetPage_EmptyIdentifier_ShouldThrow(
        string identifier)
    {
        await using var context = CreateContext();
        var repository =
            new SeasonHistoryReadRepository(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetPageAsync(
                identifier == "organization"
                    ? Guid.Empty
                    : OrganizationId,
                identifier == "land"
                    ? Guid.Empty
                    : LandId,
                identifier == "plot"
                    ? Guid.Empty
                    : null,
                includeNonTerminal: false,
                skip: 0,
                take: 20));
    }

    [Theory]
    [InlineData(-1, 20)]
    [InlineData(0, 0)]
    public async Task GetPage_InvalidWindow_ShouldThrow(
        int skip,
        int take)
    {
        await using var context = CreateContext();
        var repository =
            new SeasonHistoryReadRepository(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetPageAsync(
                OrganizationId,
                LandId,
                null,
                includeNonTerminal: false,
                skip,
                take));
    }

    private static SiPaculDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
                .UseInMemoryDatabase(
                    "season-history-" + Guid.NewGuid())
                .Options;

        return new SiPaculDbContext(options);
    }
}
