using SiPacul.Application.Security.Authorization.Services;
using SiPacul.Application.Evaluations.SeasonHistories.Persistence;
using SiPacul.Application.Security.Bootstrap.Services;
using Microsoft.AspNetCore.Identity;
using SiPacul.Application.Security.Authentication.Services;
using SiPacul.Application.Organizations.Members.Services;
using SiPacul.Infrastructure.Identity;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Application.Finance.SalePayments.Persistence;
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

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);

                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddSignInManager()
            .AddEntityFrameworkStores<SiPaculDbContext>();

        services.AddScoped<
            IUserAuthenticationService,
            UserAuthenticationService>();

        services.AddScoped<
            IOrganizationMemberService,
            OrganizationMemberService>();

        services.Configure<FirstOwnerBootstrapOptions>(
            configuration.GetSection(
                FirstOwnerBootstrapOptions.SectionName));

        services.AddScoped<
            IFirstOwnerBootstrapService,
            FirstOwnerBootstrapService>();

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

        services.AddScoped<
            ISalePaymentRepository,
            SalePaymentRepository>();

        services.AddScoped<
            ISalePaymentConfirmationProcessor,
            SalePaymentConfirmationProcessor>();

        services.AddScoped<
            IProfitabilityReadRepository,
            ProfitabilityReadRepository>();

        services.AddScoped<
            ISeasonHistoryReadRepository,
            SeasonHistoryReadRepository>();

        services.AddScoped<
            IProfitSharingSettlementRepository,
            ProfitSharingSettlementRepository>();

        services.AddScoped<
            IProfitSharingSchemeRepository,
            ProfitSharingSchemeRepository>();

        services.AddScoped<
            IProfitSharingSchemeActivationProcessor,
            ProfitSharingSchemeActivationProcessor>();

        services.AddScoped<
            IProfitSharingSchemeAssignmentRepository,
            ProfitSharingSchemeAssignmentRepository>();

        services.AddScoped<
            IProfitSharingWaterfallSettlementRepository,
            ProfitSharingWaterfallSettlementRepository>();

        services.AddScoped<
            IProfitSharingWaterfallSettlementOperationProcessor,
            ProfitSharingWaterfallSettlementOperationProcessor>();

        services.AddScoped<
            IProfitSharingSettlementFinalizationProcessor,
            ProfitSharingSettlementFinalizationProcessor>();

        services.AddScoped<
            IOrganizationPermissionService,
            OrganizationPermissionService>();

        return services;
    }
}
