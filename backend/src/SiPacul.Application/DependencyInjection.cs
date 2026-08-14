using SiPacul.Application.Finance.ProfitSharing.Services;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Services;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Services;
using SiPacul.Application.Finance.Profitability.Services;
using SiPacul.Application.Finance.SalePayments.Services;
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

        services.AddScoped<
            ISalePaymentService,
            SalePaymentService>();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<
            IProfitabilityService,
            ProfitabilityService>();

        services.AddScoped<
            IProfitSharingSettlementService,
            ProfitSharingSettlementService>();

        services.AddScoped<
            IProfitSharingSchemeService,
            ProfitSharingSchemeService>();

        services.AddScoped<
            IProfitSharingSchemeAssignmentService,
            ProfitSharingSchemeAssignmentService>();

        return services;
    }
}
