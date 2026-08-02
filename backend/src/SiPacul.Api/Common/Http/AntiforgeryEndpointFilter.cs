using Microsoft.AspNetCore.Antiforgery;
using SiPacul.Application.Security.Authentication;

namespace SiPacul.Api.Common.Http;

internal sealed class AntiforgeryEndpointFilter :
    IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var antiforgery =
            context.HttpContext.RequestServices
                .GetRequiredService<IAntiforgery>();

        try
        {
            await antiforgery.ValidateRequestAsync(
                context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Antiforgery validation failed",
                detail:
                    "The antiforgery token is missing " +
                    "or invalid.",
                extensions:
                    new Dictionary<string, object?>
                    {
                        ["code"] =
                            AuthenticationErrorCodes
                                .InvalidAntiforgeryToken
                    });
        }

        return await next(context);
    }
}
