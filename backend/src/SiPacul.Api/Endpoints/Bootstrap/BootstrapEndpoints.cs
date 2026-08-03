using SiPacul.Api.Common.Http;
using SiPacul.Api.Security;
using SiPacul.Application.Security.Bootstrap;
using SiPacul.Application.Security.Bootstrap.Contracts;
using SiPacul.Application.Security.Bootstrap.Services;

namespace SiPacul.Api.Endpoints.Bootstrap;

public static class BootstrapEndpoints
{
    private const string BasePath =
        "/api/v1/bootstrap";

    public static IEndpointRouteBuilder
        MapBootstrapEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup(BasePath)
                .WithTags("Bootstrap");

        group.MapGet(
                "/status",
                GetStatusAsync)
            .AllowAnonymous()
            .Produces<FirstOwnerBootstrapStatusResponse>(
                StatusCodes.Status200OK);

        group.MapPost(
                "/owner",
                BootstrapOwnerAsync)
            .AllowAnonymous()
            .AddEndpointFilter<
                AntiforgeryEndpointFilter>()
            .Produces<FirstOwnerBootstrapResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status401Unauthorized)
            .ProducesProblem(
                StatusCodes.Status409Conflict)
            .ProducesProblem(
                StatusCodes.Status500InternalServerError)
            .ProducesProblem(
                StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        IFirstOwnerBootstrapService bootstrapService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(context.Response);

        var status =
            await bootstrapService.GetStatusAsync(
                cancellationToken);

        return Results.Ok(status);
    }

    private static async Task<IResult> BootstrapOwnerAsync(
        FirstOwnerBootstrapRequest request,
        IFirstOwnerBootstrapService bootstrapService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(context.Response);

        var suppliedToken =
            ReadBootstrapToken(
                context.Request);

        if (suppliedToken is null)
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "Bootstrap authorization failed",
                "The bootstrap token is missing or invalid.",
                FirstOwnerBootstrapErrorCodes
                    .InvalidToken);
        }

        var result =
            await bootstrapService.BootstrapAsync(
                request,
                suppliedToken,
                cancellationToken);

        if (result.IsSuccess &&
            result.Value is not null)
        {
            return Results.Created(
                $"{BasePath}/status",
                result.Value);
        }

        return result.Failure switch
        {
            FirstOwnerBootstrapFailure.NotConfigured =>
                Problem(
                    StatusCodes.Status503ServiceUnavailable,
                    "Bootstrap unavailable",
                    result.Message ??
                        "First Owner bootstrap is not configured.",
                    FirstOwnerBootstrapErrorCodes
                        .NotConfigured),

            FirstOwnerBootstrapFailure.InvalidToken =>
                Problem(
                    StatusCodes.Status401Unauthorized,
                    "Bootstrap authorization failed",
                    "The bootstrap token is missing or invalid.",
                    FirstOwnerBootstrapErrorCodes
                        .InvalidToken),

            FirstOwnerBootstrapFailure.AlreadyInitialized =>
                Problem(
                    StatusCodes.Status409Conflict,
                    "Bootstrap already completed",
                    result.Message ??
                        "SiPacul has already been initialized.",
                    FirstOwnerBootstrapErrorCodes
                        .AlreadyInitialized),

            FirstOwnerBootstrapFailure.InvalidRequest =>
                Problem(
                    StatusCodes.Status400BadRequest,
                    "Invalid bootstrap request",
                    result.Message ??
                        "The bootstrap request is invalid.",
                    FirstOwnerBootstrapErrorCodes
                        .InvalidRequest),

            FirstOwnerBootstrapFailure
                .IdentityValidationFailed =>
                Problem(
                    StatusCodes.Status400BadRequest,
                    "Owner account validation failed",
                    result.Message ??
                        "The Owner account is invalid.",
                    FirstOwnerBootstrapErrorCodes
                        .IdentityValidationFailed,
                    result.Errors),

            FirstOwnerBootstrapFailure.Conflict =>
                Problem(
                    StatusCodes.Status409Conflict,
                    "Bootstrap conflict",
                    result.Message ??
                        "Bootstrap data conflicts with " +
                        "an existing record.",
                    FirstOwnerBootstrapErrorCodes
                        .Conflict),

            _ =>
                Problem(
                    StatusCodes.Status500InternalServerError,
                    "Bootstrap failed",
                    "Bootstrap could not be completed.",
                    FirstOwnerBootstrapErrorCodes
                        .PersistenceFailure)
        };
    }

    private static string? ReadBootstrapToken(
        HttpRequest request)
    {
        if (!request.Headers.TryGetValue(
                SiPaculBootstrapDefaults
                    .TokenHeaderName,
                out var values) ||
            values.Count != 1)
        {
            return null;
        }

        var token =
            values[0];

        if (string.IsNullOrWhiteSpace(token) ||
            token.Length >
                SiPaculBootstrapDefaults
                    .MaximumTokenHeaderLength)
        {
            return null;
        }

        return token;
    }

    private static IResult Problem(
        int statusCode,
        string title,
        string detail,
        string code,
        IReadOnlyList<string>? errors = null)
    {
        var extensions =
            new Dictionary<string, object?>
            {
                ["code"] = code
            };

        if (errors is { Count: > 0 })
        {
            extensions["errors"] = errors;
        }

        return Results.Problem(
            statusCode:
                statusCode,
            title:
                title,
            detail:
                detail,
            extensions:
                extensions);
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
