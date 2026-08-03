using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Security.Authorization.Services;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Infrastructure.Tests.Identity;

public sealed class OrganizationPermissionServiceTests
{
    [Fact]
    public void Service_ShouldBeSealedAndImplementContract()
    {
        Assert.True(
            typeof(OrganizationPermissionService)
                .IsSealed);

        Assert.True(
            typeof(IOrganizationPermissionService)
                .IsAssignableFrom(
                    typeof(
                        OrganizationPermissionService)));
    }

    [Fact]
    public void Service_ShouldRequireDbContext()
    {
        var constructor =
            Assert.Single(
                typeof(OrganizationPermissionService)
                    .GetConstructors());

        var parameter =
            Assert.Single(
                constructor.GetParameters());

        Assert.Equal(
            typeof(SiPaculDbContext),
            parameter.ParameterType);
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
                        IOrganizationPermissionService));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(OrganizationPermissionService),
            descriptor.ImplementationType);
    }

    [Fact]
    public void Contract_ShouldExposeExpectedAuthorizationMethod()
    {
        var method =
            typeof(IOrganizationPermissionService)
                .GetMethod(
                    nameof(
                        IOrganizationPermissionService
                            .HasPermissionAsync));

        Assert.NotNull(method);
        Assert.Equal(
            typeof(Task<bool>),
            method!.ReturnType);

        var parameters =
            method.GetParameters();

        Assert.Equal(4, parameters.Length);
        Assert.Equal(
            typeof(Guid),
            parameters[0].ParameterType);

        Assert.Equal(
            typeof(Guid),
            parameters[1].ParameterType);

        Assert.Equal(
            typeof(string),
            parameters[2].ParameterType);

        Assert.Equal(
            typeof(CancellationToken),
            parameters[3].ParameterType);
    }

    private static IConfiguration
        CreateConfiguration()
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

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
