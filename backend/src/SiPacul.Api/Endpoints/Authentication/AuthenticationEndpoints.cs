using Microsoft.AspNetCore.Antiforgery;
using SiPacul.Api.Common.Http;
using SiPacul.Api.Security;
using SiPacul.Api.Security.RateLimiting;
using SiPacul.Application.Security.Authentication;
using SiPacul.Application.Security.Authentication.Contracts;
using SiPacul.Application.Security.Authentication.Services;

namespace SiPacul.Api.Endpoints.Authentication;

public static class AuthenticationEndpoints
{
    private const string BasePath =
        "/api/v1/auth";

    public static IEndpointRouteBuilder
        MapAuthenticationEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup(BasePath)
                .WithTags("Authentication");

        group.MapGet(
                "/csrf",
                GetAntiforgeryToken)
            .AllowAnonymous();

        group.MapPost(
                "/login",
                LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(
                SiPaculRateLimitingDefaults
                    .AuthenticationPolicyName)
            .AddEndpointFilter<
                AntiforgeryEndpointFilter>()
            .ProducesProblem(
                StatusCodes.Status429TooManyRequests);

        group.MapPost(
                "/logout",
                LogoutAsync)
            .RequireAuthorization()
            .AddEndpointFilter<
                AntiforgeryEndpointFilter>();

        group.MapGet(
                "/me",
                GetCurrentUserAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static IResult GetAntiforgeryToken(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        SetNoStoreHeaders(context.Response);

        var tokens =
            antiforgery.GetAndStoreTokens(context);

        if (string.IsNullOrWhiteSpace(
                tokens.RequestToken))
        {
            throw new InvalidOperationException(
                "Unable to generate antiforgery token.");
        }

        return Results.Ok(
            new AntiforgeryTokenResponse(
                tokens.RequestToken,
                SiPaculAuthenticationDefaults
                    .AntiforgeryHeaderName));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IUserAuthenticationService authenticationService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(context.Response);

        var validationProblem =
            ValidateLoginRequest(request);

        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var normalizedRequest =
            request with
            {
                Email = request.Email!.Trim()
            };

        var result =
            await authenticationService.LoginAsync(
                normalizedRequest,
                cancellationToken);

        if (!result.Succeeded || result.User is null)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Authentication failed",
                detail:
                    "Email or password is invalid.",
                extensions:
                    new Dictionary<string, object?>
                    {
                        ["code"] =
                            AuthenticationErrorCodes
                                .InvalidCredentials
                    });
        }

        return Results.Ok(result.User);
    }

    private static async Task<IResult> LogoutAsync(
        IUserAuthenticationService authenticationService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(context.Response);

        await authenticationService.SignOutAsync(
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        IUserAuthenticationService authenticationService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(context.Response);

        var user =
            await authenticationService.GetCurrentUserAsync(
                context.User,
                cancellationToken);

        if (user is null)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Authentication required",
                detail:
                    "The current authentication session " +
                    "is not valid.",
                extensions:
                    new Dictionary<string, object?>
                    {
                        ["code"] =
                            AuthenticationErrorCodes
                                .Unauthenticated
                    });
        }

        return Results.Ok(user);
    }

    private static IResult? ValidateLoginRequest(
        LoginRequest request)
    {
        if (request is null)
        {
            return InvalidRequest(
                "Login request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return InvalidRequest(
                "Email is required.");
        }

        if (request.Email.Trim().Length >
            LoginRequest.MaxEmailLength)
        {
            return InvalidRequest(
                "Email is too long.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return InvalidRequest(
                "Password is required.");
        }

        if (request.Password.Length >
            LoginRequest.MaxPasswordLength)
        {
            return InvalidRequest(
                "Password is too long.");
        }

        return null;
    }

    private static IResult InvalidRequest(
        string detail)
    {
        return Results.Problem(
            statusCode:
                StatusCodes.Status400BadRequest,
            title:
                "Invalid authentication request",
            detail:
                detail,
            extensions:
                new Dictionary<string, object?>
                {
                    ["code"] =
                        AuthenticationErrorCodes
                            .InvalidRequest
                });
    }

    private static void SetNoStoreHeaders(
        HttpResponse response)
    {
        response.Headers["Cache-Control"] =
            "no-store";

        response.Headers["Pragma"] =
            "no-cache";
    }
}
