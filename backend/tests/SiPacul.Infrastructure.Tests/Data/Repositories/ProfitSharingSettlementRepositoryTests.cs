using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Data.Repositories;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Repositories;

public sealed class
    ProfitSharingSettlementRepositoryTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Fact]
    public void Repository_ShouldImplementContract()
    {
        Assert.IsAssignableFrom<
            IProfitSharingSettlementRepository>(
                CreateRepository(
                    out _));
    }

    [Fact]
    public void Repository_ShouldBeSealed()
    {
        Assert.True(
            typeof(ProfitSharingSettlementRepository)
                .IsSealed);
    }

    [Fact]
    public void Repository_ShouldHaveDbContextConstructor()
    {
        var constructor =
            typeof(ProfitSharingSettlementRepository)
                .GetConstructors()
                .Single();

        var parameter =
            constructor.GetParameters().Single();

        Assert.Equal(
            typeof(SiPaculDbContext),
            parameter.ParameterType);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProfitSharingSettlementRepository(
                null!));
    }

    [Fact]
    public void Add_ShouldTrackRootAndAllocations()
    {
        var repository =
            CreateRepository(
                out var context);

        using (context)
        {
            var settlement = CreateSettlement();

            repository.Add(settlement);

            Assert.Equal(
                EntityState.Added,
                context.Entry(settlement).State);

            Assert.Equal(
                settlement.Allocations.Count,
                context.ChangeTracker
                    .Entries<ProfitSharingAllocation>()
                    .Count());

            Assert.All(
                context.ChangeTracker
                    .Entries<ProfitSharingAllocation>(),
                entry =>
                    Assert.Equal(
                        EntityState.Added,
                        entry.State));
        }
    }

    [Fact]
    public void Add_WithNullSettlement_ShouldThrow()
    {
        var repository =
            CreateRepository(
                out var context);

        using (context)
        {
            Assert.Throws<ArgumentNullException>(() =>
                repository.Add(null!));
        }
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterRepositoryAsScoped()
    {
        var services =
            new ServiceCollection();

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [
                            "ConnectionStrings:" +
                            "DefaultConnection"
                        ] =
                            "Host=localhost;" +
                            "Port=5432;" +
                            "Database=sipacul_tests;" +
                            "Username=sipacul;" +
                            "Password=sipacul"
                    })
                .Build();

        services.AddInfrastructure(configuration);

        var descriptor =
            services.Single(service =>
                service.ServiceType ==
                    typeof(
                        IProfitSharingSettlementRepository));

        Assert.Equal(
            typeof(ProfitSharingSettlementRepository),
            descriptor.ImplementationType);

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    private static ProfitSharingSettlementRepository
        CreateRepository(
            out SiPaculDbContext context)
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Port=5432;" +
                    "Database=sipacul_repository_tests;" +
                    "Username=sipacul;" +
                    "Password=sipacul")
                .Options;

        context =
            new SiPaculDbContext(options);

        return new ProfitSharingSettlementRepository(
            context);
    }

    private static ProfitSharingSettlement
        CreateSettlement()
    {
        var report =
            CropCycleProfitabilityReport.Calculate(
                new CropCycleProfitabilityInput(
                    OrganizationId,
                    CropCycleId,
                    "CC-001",
                    "Musim Padi",
                    CommodityId,
                    "PADI",
                    "Padi",
                    600,
                    600,
                    200,
                    100,
                    200,
                    100,
                    0,
                    new DateTime(
                        2027,
                        7,
                        10,
                        8,
                        0,
                        0,
                        DateTimeKind.Utc)));

        var calculation =
            ProfitSharingCalculator.Calculate(
                report,
                "MITRA-001",
                "Mitra Pengelola",
                [
                    new ProfitSharingContributorInput(
                        "INV-001",
                        "Investor Utama",
                        CapitalContributorRole.Investor,
                        200),
                    new ProfitSharingContributorInput(
                        "MITRA-001",
                        "Mitra Pengelola",
                        CapitalContributorRole.Partner,
                        100)
                ]);

        return ProfitSharingSettlement.CreateDraft(
            OrganizationId,
            CropCycleId,
            "SET-001",
            new DateOnly(2027, 7, 10),
            "MITRA-001",
            "Mitra Pengelola",
            calculation,
            null);
    }
}
