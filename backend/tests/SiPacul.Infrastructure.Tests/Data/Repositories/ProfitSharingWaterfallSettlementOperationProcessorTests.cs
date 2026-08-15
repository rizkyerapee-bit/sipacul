using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Calculations;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Infrastructure;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Data.Repositories;

namespace SiPacul.Infrastructure.Tests.Data.Repositories;

public sealed class
    ProfitSharingWaterfallSettlementOperationProcessorTests
{
    [Fact]
    public void Processor_ShouldBeSealedAndImplementContract()
    {
        var type =
            typeof(ProfitSharingWaterfallSettlementOperationProcessor);

        Assert.True(type.IsSealed);
        Assert.True(
            typeof(IProfitSharingWaterfallSettlementOperationProcessor)
                .IsAssignableFrom(type));
    }

    [Fact]
    public void Processor_ShouldHaveTransactionSourceDependencies()
    {
        var parameterTypes =
            typeof(ProfitSharingWaterfallSettlementOperationProcessor)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

        Assert.Contains(typeof(SiPaculDbContext), parameterTypes);
        Assert.Contains(
            typeof(IProfitSharingWaterfallSettlementRepository),
            parameterTypes);
        Assert.Contains(
            typeof(IProfitSharingSchemeAssignmentRepository),
            parameterTypes);
        Assert.Contains(
            typeof(IProfitabilityReadRepository),
            parameterTypes);
        Assert.Contains(
            typeof(ICapitalContributionRepository),
            parameterTypes);
        Assert.Contains(typeof(TimeProvider), parameterTypes);
    }

    [Fact]
    public void Processor_ShouldExposeFinalizeAndVoidOperations()
    {
        var contract =
            typeof(IProfitSharingWaterfallSettlementOperationProcessor);

        Assert.NotNull(contract.GetMethod("FinalizeAsync"));
        Assert.NotNull(contract.GetMethod("VoidAsync"));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterProcessorAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;" +
                        "Database=sipacul_tests;" +
                        "Username=sipacul;Password=sipacul"
                })
            .Build();

        services.AddInfrastructure(configuration);

        var descriptor = services.Single(service =>
            service.ServiceType ==
                typeof(
                    IProfitSharingWaterfallSettlementOperationProcessor));

        Assert.Equal(
            typeof(ProfitSharingWaterfallSettlementOperationProcessor),
            descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Failed_WithNone_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProfitSharingWaterfallSettlementOperationResult.Failed(
                ProfitSharingWaterfallSettlementFailure.None));
    }

    [Fact]
    public void SourceCalculationFailed_WithNone_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProfitSharingWaterfallSourceCalculation.Failed(
                ProfitSharingWaterfallSourceFailure.None));
    }

    [Theory]
    [InlineData(0, ProfitSharingWaterfallSettlementFailure.None)]
    [InlineData(1, ProfitSharingWaterfallSettlementFailure.CropCycleNotFound)]
    [InlineData(2, ProfitSharingWaterfallSettlementFailure.AssignmentNotFound)]
    [InlineData(3, ProfitSharingWaterfallSettlementFailure.SettlementNotFound)]
    [InlineData(4, ProfitSharingWaterfallSettlementFailure.CodeAlreadyExists)]
    [InlineData(5, ProfitSharingWaterfallSettlementFailure.ActiveSettlementExists)]
    [InlineData(6, ProfitSharingWaterfallSettlementFailure.CropCycleNotTerminal)]
    [InlineData(7, ProfitSharingWaterfallSettlementFailure.ActiveActivityExists)]
    [InlineData(8, ProfitSharingWaterfallSettlementFailure.DraftHarvestExists)]
    [InlineData(9, ProfitSharingWaterfallSettlementFailure.UnsoldHarvestExists)]
    [InlineData(10, ProfitSharingWaterfallSettlementFailure.DraftSaleExists)]
    [InlineData(11, ProfitSharingWaterfallSettlementFailure.OutstandingReceivableExists)]
    [InlineData(12, ProfitSharingWaterfallSettlementFailure.DraftExpenseExists)]
    [InlineData(13, ProfitSharingWaterfallSettlementFailure.DraftContributionExists)]
    [InlineData(14, ProfitSharingWaterfallSettlementFailure.DraftPaymentExists)]
    [InlineData(15, ProfitSharingWaterfallSettlementFailure.CapitalDoesNotMatchCost)]
    [InlineData(16, ProfitSharingWaterfallSettlementFailure.ZeroCostUnsupported)]
    [InlineData(17, ProfitSharingWaterfallSettlementFailure.CapitalIdentityConflict)]
    [InlineData(18, ProfitSharingWaterfallSettlementFailure.CapitalNotInScheme)]
    [InlineData(19, ProfitSharingWaterfallSettlementFailure.CapitalRoleMismatch)]
    [InlineData(20, ProfitSharingWaterfallSettlementFailure.SourceDataChanged)]
    [InlineData(21, ProfitSharingWaterfallSettlementFailure.CalculationUnavailable)]
    [InlineData(22, ProfitSharingWaterfallSettlementFailure.InvalidStatus)]
    [InlineData(23, ProfitSharingWaterfallSettlementFailure.ConcurrencyConflict)]
    [InlineData(24, ProfitSharingWaterfallSettlementFailure.Validation)]
    public void FailureValues_ShouldRemainStable(
        int expectedValue,
        ProfitSharingWaterfallSettlementFailure failure)
    {
        Assert.Equal(expectedValue, (int)failure);
    }
}
