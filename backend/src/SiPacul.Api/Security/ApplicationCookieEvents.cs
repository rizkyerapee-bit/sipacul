using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Api.Security;

public sealed class ApplicationCookieEvents :
    CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        await SecurityStampValidator
            .ValidatePrincipalAsync(context);

        if (context.Principal?.Identity?.IsAuthenticated !=
            true)
        {
            return;
        }

        var userManager =
            context.HttpContext.RequestServices
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var user =
            await userManager.GetUserAsync(
                context.Principal);

        if (user is not null && user.IsActive)
        {
            return;
        }

        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(
            IdentityConstants.ApplicationScheme);
    }

    public override Task RedirectToLogin(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        return Task.CompletedTask;
    }
}
