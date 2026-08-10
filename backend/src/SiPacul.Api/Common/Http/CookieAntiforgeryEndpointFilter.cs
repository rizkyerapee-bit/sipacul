using SiPacul.Api.Security;

namespace SiPacul.Api.Common.Http;

internal sealed class CookieAntiforgeryEndpointFilter :
    IEndpointFilter
{
    private readonly AntiforgeryEndpointFilter _antiforgery =
        new();

    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!request.Cookies.ContainsKey(
                SiPaculAuthenticationDefaults
                    .AuthenticationCookieName))
        {
            return next(context);
        }

        return _antiforgery.InvokeAsync(
            context,
            next);
    }
}
