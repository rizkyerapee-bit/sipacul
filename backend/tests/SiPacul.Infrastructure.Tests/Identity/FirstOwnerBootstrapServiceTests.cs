using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiPacul.Application.Security.Bootstrap.Services;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Infrastructure.Tests.Identity;

public sealed class FirstOwnerBootstrapServiceTests
{
    [Fact]
    public void Options_ShouldReportConfigurationState()
    {
        var missing =
            new FirstOwnerBootstrapOptions();

        var shortToken =
            new FirstOwnerBootstrapOptions
            {
                OwnerToken = "too-short"
            };

        var configured =
            new FirstOwnerBootstrapOptions
            {
                OwnerToken =
                    "0123456789abcdef0123456789abcdef"
            };

        Assert.False(missing.IsConfigured);
        Assert.False(shortToken.IsConfigured);
        Assert.True(configured.IsConfigured);

        Assert.Equal(
            32,
            FirstOwnerBootstrapOptions
                .MinimumTokenLength);
    }

    [Fact]
    public void Service_ShouldBeSealedAndImplementContract()
    {
        Assert.True(
            typeof(FirstOwnerBootstrapService)
                .IsSealed);

        Assert.True(
            typeof(IFirstOwnerBootstrapService)
                .IsAssignableFrom(
                    typeof(
                        FirstOwnerBootstrapService)));
    }

    [Fact]
    public void Service_ShouldHaveExpectedDependencies()
    {
        var constructor =
            typeof(FirstOwnerBootstrapService)
                .GetConstructors()
                .Single();

        var parameterTypes =
            constructor
                .GetParameters()
                .Select(parameter =>
                    parameter.ParameterType)
                .ToArray();

        Assert.Contains(
            typeof(UserManager<ApplicationUser>),
            parameterTypes);

        Assert.Contains(
            typeof(SiPaculDbContext),
            parameterTypes);

        Assert.Contains(
            typeof(
                IOptions<
                    FirstOwnerBootstrapOptions>),
            parameterTypes);

        Assert.Contains(
            typeof(
                ILogger<
                    FirstOwnerBootstrapService>),
            parameterTypes);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterServiceAsScoped()
    {
        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            CreateConfiguration());

        var descriptor =
            services.Single(candidate =>
                candidate.ServiceType ==
                    typeof(
                        IFirstOwnerBootstrapService));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(FirstOwnerBootstrapService),
            descriptor.ImplementationType);
    }

    [Fact]
    public void AddInfrastructure_ShouldBindBootstrapToken()
    {
        const string token =
            "0123456789abcdef0123456789abcdef";

        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            CreateConfiguration(token));

        using var provider =
            services.BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                IOptions<
                    FirstOwnerBootstrapOptions>>()
                .Value;

        Assert.Equal(
            token,
            options.OwnerToken);

        Assert.True(options.IsConfigured);
    }

    private static IConfiguration
        CreateConfiguration(
            string? bootstrapToken = null)
    {
        var values =
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;" +
                    "Port=5433;" +
                    "Database=sipacul_tests;" +
                    "Username=sipacul_test;" +
                    "Password=sipacul_test"
            };

        if (bootstrapToken is not null)
        {
            values[
                "Bootstrap:OwnerToken"] =
                bootstrapToken;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
