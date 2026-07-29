using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Common.Persistence;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.MasterData.CommodityCategories.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Data.Repositories;

namespace SiPacul.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' " +
                "has not been configured.");
        }

        services.AddDbContext<SiPaculDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(SiPaculDbContext)
                            .Assembly
                            .GetName()
                            .Name!);

                    npgsqlOptions.EnableRetryOnFailure(
                        5,
                        TimeSpan.FromSeconds(10),
                        null);
                });
        });

        services.AddScoped<
            IOrganizationRepository,
            OrganizationRepository>();

        services.AddScoped<
            ICommodityCategoryRepository,
            CommodityCategoryRepository>();

        services.AddScoped<
            ICommodityRepository,
            CommodityRepository>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

        return services;
    }
}
