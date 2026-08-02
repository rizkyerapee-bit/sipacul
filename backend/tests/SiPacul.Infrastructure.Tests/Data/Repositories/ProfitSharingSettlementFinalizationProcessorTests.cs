using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Infrastructure;
using SiPacul.Infrastructure.Data.Repositories;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Repositories;

public sealed class
    ProfitSharingSettlementFinalizationProcessorTests
{
    [Fact]
    public void Processor_ShouldBeSealed()
    {
        Assert.True(
            typeof(
                ProfitSharingSettlementFinalizationProcessor)
                .IsSealed);
    }

    [Fact]
    public void Processor_ShouldImplementContract()
    {
        Assert.True(
            typeof(
                IProfitSharingSettlementFinalizationProcessor)
                .IsAssignableFrom(
                    typeof(
                        ProfitSharingSettlementFinalizationProcessor)));
    }

    [Fact]
    public void Processor_ShouldHaveExpectedConstructorDependencies()
    {
        var constructor =
            typeof(
                ProfitSharingSettlementFinalizationProcessor)
                .GetConstructors()
                .Single();

        var parameterTypes =
            constructor
                .GetParameters()
                .Select(parameter =>
                    parameter.ParameterType)
                .ToArray();

        Assert.Contains(
            typeof(
                IProfitSharingSettlementRepository),
            parameterTypes);

        Assert.Contains(
            typeof(
                IProfitSharingSettlementFinalizationProcessor)
                .Assembly
                .GetType(
                    "SiPacul.Application.Finance." +
                    "CapitalContributions.Persistence." +
                    "ICapitalContributionRepository")!,
            parameterTypes);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterProcessorAsScoped()
    {
        var services = new ServiceCollection();

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
                        IProfitSharingSettlementFinalizationProcessor));

        Assert.Equal(
            typeof(
                ProfitSharingSettlementFinalizationProcessor),
            descriptor.ImplementationType);

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public void FinalizationResult_Success_ShouldExposeSettlement()
    {
        var settlement =
            ProfitSharingFinalizationTestData
                .CreateSettlement();

        settlement.FinalizeSettlement();

        var result =
            ProfitSharingFinalizationResult.Succeeded(
                settlement);

        Assert.True(result.IsSuccess);
        Assert.Same(settlement, result.Settlement);

        Assert.Equal(
            ProfitSharingFinalizationFailure.None,
            result.Failure);
    }

    [Fact]
    public void FinalizationResult_FailedWithNone_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure.None));
    }

    [Theory]
    [InlineData(
        0,
        ProfitSharingFinalizationFailure.None)]
    [InlineData(
        1,
        ProfitSharingFinalizationFailure.SettlementNotFound)]
    [InlineData(
        2,
        ProfitSharingFinalizationFailure.InvalidStatus)]
    [InlineData(
        3,
        ProfitSharingFinalizationFailure.ActiveSettlementExists)]
    [InlineData(
        4,
        ProfitSharingFinalizationFailure.CropCycleNotTerminal)]
    [InlineData(
        5,
        ProfitSharingFinalizationFailure.ActiveActivityExists)]
    [InlineData(
        6,
        ProfitSharingFinalizationFailure.DraftHarvestExists)]
    [InlineData(
        7,
        ProfitSharingFinalizationFailure.UnsoldHarvestExists)]
    [InlineData(
        8,
        ProfitSharingFinalizationFailure.DraftSaleExists)]
    [InlineData(
        9,
        ProfitSharingFinalizationFailure
            .OutstandingReceivableExists)]
    [InlineData(
        10,
        ProfitSharingFinalizationFailure.DraftExpenseExists)]
    [InlineData(
        11,
        ProfitSharingFinalizationFailure
            .DraftContributionExists)]
    [InlineData(
        12,
        ProfitSharingFinalizationFailure.DraftPaymentExists)]
    [InlineData(
        13,
        ProfitSharingFinalizationFailure
            .CapitalDoesNotMatchCost)]
    [InlineData(
        14,
        ProfitSharingFinalizationFailure.ZeroCostUnsupported)]
    [InlineData(
        15,
        ProfitSharingFinalizationFailure.SourceDataChanged)]
    [InlineData(
        16,
        ProfitSharingFinalizationFailure.ConcurrencyConflict)]
    public void FailureValues_ShouldRemainStable(
        int expectedValue,
        ProfitSharingFinalizationFailure failure)
    {
        Assert.Equal(expectedValue, (int)failure);
    }

    private static class
        ProfitSharingFinalizationTestData
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

        public static Domain.Entities.Finance.ProfitSharing
            .ProfitSharingSettlement CreateSettlement()
        {
            var report =
                Domain.Entities.Finance.Profitability
                    .CropCycleProfitabilityReport.Calculate(
                        new Domain.Entities.Finance.Profitability
                            .CropCycleProfitabilityInput(
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
                                    5,
                                    20,
                                    8,
                                    0,
                                    0,
                                    DateTimeKind.Utc)));

            var calculation =
                Domain.Entities.Finance.ProfitSharing
                    .ProfitSharingCalculator.Calculate(
                        report,
                        "MITRA-001",
                        "Mitra Pengelola",
                        [
                            new Domain.Entities.Finance
                                .ProfitSharing
                                .ProfitSharingContributorInput(
                                    "INV-001",
                                    "Investor Utama",
                                    Domain.Entities.Finance
                                        .CapitalContributorRole
                                        .Investor,
                                    200),
                            new Domain.Entities.Finance
                                .ProfitSharing
                                .ProfitSharingContributorInput(
                                    "MITRA-001",
                                    "Mitra Pengelola",
                                    Domain.Entities.Finance
                                        .CapitalContributorRole
                                        .Partner,
                                    100)
                        ]);

            return Domain.Entities.Finance.ProfitSharing
                .ProfitSharingSettlement.CreateDraft(
                    OrganizationId,
                    CropCycleId,
                    "SET-001",
                    new DateOnly(2027, 5, 20),
                    "MITRA-001",
                    "Mitra Pengelola",
                    calculation,
                    null);
        }
    }
}
