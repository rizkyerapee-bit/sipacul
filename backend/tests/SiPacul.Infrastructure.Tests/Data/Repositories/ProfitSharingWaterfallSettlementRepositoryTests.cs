using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Data.Repositories;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Repositories;

public sealed class ProfitSharingWaterfallSettlementRepositoryTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public void Repository_ShouldImplementContract()
    {
        var repository = CreateRepository(out var context);

        using (context)
        {
            Assert.IsAssignableFrom<
                IProfitSharingWaterfallSettlementRepository>(repository);
        }
    }

    [Fact]
    public void Repository_ShouldBeSealed()
    {
        Assert.True(
            typeof(ProfitSharingWaterfallSettlementRepository).IsSealed);
    }

    [Fact]
    public void Repository_ShouldHaveDbContextConstructor()
    {
        var constructor =
            typeof(ProfitSharingWaterfallSettlementRepository)
                .GetConstructors()
                .Single();

        Assert.Equal(
            typeof(SiPaculDbContext),
            constructor.GetParameters().Single().ParameterType);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldReject()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProfitSharingWaterfallSettlementRepository(null!));
    }

    [Fact]
    public void Add_ShouldTrackCompleteAggregate()
    {
        var repository = CreateRepository(out var context);

        using (context)
        {
            var settlement = CreateSettlement();

            repository.Add(settlement);

            Assert.Equal(
                EntityState.Added,
                context.Entry(settlement).State);
            Assert.Single(
                context.ChangeTracker.Entries<
                    ProfitSharingWaterfallParticipantAllocation>());
            Assert.Empty(
                context.ChangeTracker.Entries<
                    ProfitSharingWaterfallPriorityAllocation>());
            Assert.Empty(
                context.ChangeTracker.Entries<
                    ProfitSharingWaterfallResidualShareSnapshot>());
        }
    }

    [Fact]
    public void Add_WithNullSettlement_ShouldReject()
    {
        var repository = CreateRepository(out var context);

        using (context)
        {
            Assert.Throws<ArgumentNullException>(() =>
                repository.Add(null!));
        }
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
                typeof(IProfitSharingWaterfallSettlementRepository));

        Assert.Equal(
            typeof(ProfitSharingWaterfallSettlementRepository),
            descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private static ProfitSharingWaterfallSettlementRepository
        CreateRepository(out SiPaculDbContext context)
    {
        var options = new DbContextOptionsBuilder<SiPaculDbContext>()
            .UseNpgsql(
                "Host=localhost;" +
                "Port=5432;" +
                "Database=sipacul_waterfall_repository_tests;" +
                "Username=sipacul;" +
                "Password=sipacul")
            .Options;

        context = new SiPaculDbContext(options);
        return new ProfitSharingWaterfallSettlementRepository(context);
    }

    private static ProfitSharingWaterfallSettlement CreateSettlement()
    {
        var participant = new ProfitSharingSchemeParticipantDefinition(
            "PERUSAHAAN",
            "Perusahaan",
            ProfitSharingParticipantRole.Company,
            true,
            1);

        var scheme = ProfitSharingScheme.CreateDraft(
            OrganizationId,
            "INTERNAL",
            "Internal Perusahaan",
            null,
            [participant],
            [],
            ProfitSharingResidualMethod.RemainderToParticipant,
            "PERUSAHAAN",
            []);
        scheme.Activate();

        var assignment = ProfitSharingSchemeAssignment.Create(
            OrganizationId,
            CropCycleId,
            scheme);

        var generatedAt = new DateTime(
            2027,
            7,
            24,
            8,
            0,
            0,
            DateTimeKind.Utc);

        var report = CropCycleProfitabilityReport.Calculate(
            new CropCycleProfitabilityInput(
                OrganizationId,
                CropCycleId,
                "CYCLE-001",
                "Siklus Uji",
                Guid.NewGuid(),
                "CABAI",
                "Cabai",
                150_000m,
                150_000m,
                100_000m,
                0m,
                100_000m,
                0m,
                0m,
                generatedAt));

        var calculation = ProfitSharingWaterfallCalculator.Calculate(
            report,
            new ProfitSharingWaterfallSchemeInput(
                [
                    new ProfitSharingWaterfallParticipantInput(
                        "PERUSAHAAN",
                        "Perusahaan",
                        ProfitSharingParticipantRole.Company,
                        100_000m,
                        true,
                        1)
                ],
                [],
                ProfitSharingResidualPolicyInput.RemainderToParticipant(
                    "PERUSAHAAN")));

        return ProfitSharingWaterfallSettlement.CreateFinalized(
            OrganizationId,
            CropCycleId,
            "SET-001",
            new DateOnly(2027, 7, 24),
            assignment,
            report,
            calculation,
            null,
            generatedAt.AddHours(1));
    }
}
