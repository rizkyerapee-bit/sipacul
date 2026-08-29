using Microsoft.AspNetCore.Diagnostics;

namespace SiPacul.Api.Common.Http;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public const string ErrorCode =
        "Server.UnexpectedError";
    public const string InvalidRequestErrorCode =
        "Request.Invalid";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken _)
    {
        if (exception is
            Microsoft.AspNetCore.Http.BadHttpRequestException
                badRequestException)
        {
            var clientStatusCode =
                badRequestException.StatusCode >=
                    StatusCodes.Status400BadRequest &&
                badRequestException.StatusCode <
                    StatusCodes.Status500InternalServerError
                    ? badRequestException.StatusCode
                    : StatusCodes.Status400BadRequest;

            logger.LogWarning(
                "Rejected API request for {RequestMethod} " +
                "{RequestPath} with status {StatusCode}. " +
                "Trace identifier: {TraceIdentifier}.",
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                clientStatusCode,
                httpContext.TraceIdentifier);

            return await WriteProblemAsync(
                httpContext,
                clientStatusCode,
                "Invalid request",
                "The request could not be processed.",
                InvalidRequestErrorCode);
        }

        logger.LogError(
            exception,
            "Unhandled API exception for {RequestMethod} " +
            "{RequestPath}. Trace identifier: {TraceIdentifier}.",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            httpContext.TraceIdentifier);

        return await WriteProblemAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Unexpected server error",
            "An unexpected server error occurred.",
            ErrorCode);
    }

    private static async ValueTask<bool> WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string code)
    {
        httpContext.Response.Headers["Cache-Control"] =
            "no-store";
        httpContext.Response.Headers["Pragma"] =
            "no-cache";

        var problem = Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            extensions:
                new Dictionary<string, object?>
                {
                    ["code"] = code,
                    ["traceId"] =
                        httpContext.TraceIdentifier
                });

        await problem.ExecuteAsync(httpContext);

        return true;
    }
}

public static class
    GlobalExceptionHandlingServiceCollectionExtensions
{
    public static IServiceCollection
        AddGlobalExceptionHandling(
            this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<
            GlobalExceptionHandler>();

        return services;
    }
}
