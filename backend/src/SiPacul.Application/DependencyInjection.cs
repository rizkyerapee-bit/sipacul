using SiPacul.Application.Finance.CapitalContributions.Services;
using SiPacul.Application.Finance.Expenses.Services;
using SiPacul.Application.Sales.Services;
using SiPacul.Application.Harvests.Services;
using SiPacul.Application.Cultivation.Activities.Services;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Cultivation.CropCycles.Services;
using SiPacul.Application.Cultivation.Sops.Services;
using SiPacul.Application.Lands.Services;
using SiPacul.Application.MasterData.Commodities.Services;
using SiPacul.Application.MasterData.CommodityCategories.Services;
using SiPacul.Application.Organizations.Services;

namespace SiPacul.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IOrganizationService,
            OrganizationService>();

        services.AddScoped<
            ICommodityCategoryService,
            CommodityCategoryService>();

        services.AddScoped<
            ICommodityService,
            CommodityService>();

        services.AddScoped<
            ICultivationSopService,
            CultivationSopService>();

        services.AddScoped<
            ILandService,
            LandService>();

        services.AddScoped<
            ICropCycleService,
            CropCycleService>();

        services.AddScoped<
            ICultivationActivityService,
            CultivationActivityService>();

        services.AddScoped<
            IHarvestBatchService,
            HarvestBatchService>();

        services.AddScoped<
            ISaleService,
            SaleService>();

        services.AddScoped<
            ICultivationExpenseService,
            CultivationExpenseService>();

        services.AddScoped<
            ICapitalContributionService,
            CapitalContributionService>();

        return services;
    }
}
