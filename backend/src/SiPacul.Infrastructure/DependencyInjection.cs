using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.Expenses.Persistence;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Application.Harvests.Persistence;
using SiPacul.Application.Cultivation.Activities.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.Lands.Persistence;
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
            ICultivationSopRepository,
            CultivationSopRepository>();

        services.AddScoped<
            ILandRepository,
            LandRepository>();

        services.AddScoped<
            ICropCycleRepository,
            CropCycleRepository>();

        services.AddScoped<
            ICultivationActivityRepository,
            CultivationActivityRepository>();

        services.AddScoped<
            IHarvestBatchRepository,
            HarvestBatchRepository>();

        services.AddScoped<
            ISaleRepository,
            SaleRepository>();

        services.AddScoped<
            ISaleConfirmationProcessor,
            SaleConfirmationProcessor>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

        services.AddScoped<
            ICultivationExpenseRepository,
            CultivationExpenseRepository>();

        services.AddScoped<
            ICapitalContributionRepository,
            CapitalContributionRepository>();

        return services;
    }
}
