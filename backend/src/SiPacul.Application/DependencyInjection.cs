using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
