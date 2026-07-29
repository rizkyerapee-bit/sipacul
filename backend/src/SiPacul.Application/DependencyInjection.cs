using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
