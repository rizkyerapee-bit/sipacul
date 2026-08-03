using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SiPacul.Application.Organizations.Members.Services;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Infrastructure.Tests.Identity;

public sealed class OrganizationMemberServiceTests
{
    [Fact]
    public void Service_ShouldBeSealedAndImplementContract()
    {
        Assert.True(typeof(OrganizationMemberService).IsSealed);

        Assert.True(
            typeof(IOrganizationMemberService)
                .IsAssignableFrom(
                    typeof(OrganizationMemberService)));
    }

    [Fact]
    public void Service_ShouldHaveExpectedDependencies()
    {
        var constructor = Assert.Single(
            typeof(OrganizationMemberService)
                .GetConstructors());

        var parameterTypes = constructor
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(
            typeof(UserManager<ApplicationUser>),
            parameterTypes);

        Assert.Contains(
            typeof(SiPaculDbContext),
            parameterTypes);

        Assert.Contains(
            typeof(ILogger<OrganizationMemberService>),
            parameterTypes);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration());

        var descriptor = services.Single(candidate =>
            candidate.ServiceType ==
                typeof(IOrganizationMemberService));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(OrganizationMemberService),
            descriptor.ImplementationType);
    }

    [Fact]
    public void Contract_ShouldExposeSixOperations()
    {
        var methods = typeof(IOrganizationMemberService)
            .GetMethods()
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "ActivateAsync",
                "ChangeRoleAsync",
                "CreateAsync",
                "GetAllAsync",
                "GetByIdAsync",
                "SuspendAsync"
            },
            methods);
    }

    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;" +
                "Port=5433;" +
                "Database=sipacul_tests;" +
                "Username=sipacul_test;" +
                "Password=sipacul_test"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
