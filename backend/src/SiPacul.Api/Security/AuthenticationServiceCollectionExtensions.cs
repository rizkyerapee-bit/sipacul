using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace SiPacul.Api.Security;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection
        AddSiPaculAuthentication(
            this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ApplicationCookieEvents>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    IdentityConstants.ApplicationScheme;

                options.DefaultChallengeScheme =
                    IdentityConstants.ApplicationScheme;

                options.DefaultSignInScheme =
                    IdentityConstants.ApplicationScheme;
            })
            .AddCookie(
                IdentityConstants.ApplicationScheme,
                options =>
                {
                    options.Cookie.Name =
                        SiPaculAuthenticationDefaults
                            .AuthenticationCookieName;

                    options.Cookie.HttpOnly = true;

                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.Always;

                    options.Cookie.SameSite =
                        SameSiteMode.Lax;

                    options.Cookie.Path = "/";

                    options.Cookie.IsEssential = true;

                    options.ExpireTimeSpan =
                        SiPaculAuthenticationDefaults
                            .CookieLifetime;

                    options.SlidingExpiration = true;

                    options.EventsType =
                        typeof(ApplicationCookieEvents);
                });

        services.AddAuthorization();

        services.AddAntiforgery(options =>
        {
            options.HeaderName =
                SiPaculAuthenticationDefaults
                    .AntiforgeryHeaderName;

            options.Cookie.Name =
                SiPaculAuthenticationDefaults
                    .AntiforgeryCookieName;

            options.Cookie.HttpOnly = true;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;

            options.Cookie.SameSite =
                SameSiteMode.Lax;

            options.Cookie.Path = "/";

            options.Cookie.IsEssential = true;
        });

        services.Configure<SecurityStampValidatorOptions>(
            options =>
            {
                options.ValidationInterval =
                    SiPaculAuthenticationDefaults
                        .SecurityStampValidationInterval;
            });

        return services;
    }
}
