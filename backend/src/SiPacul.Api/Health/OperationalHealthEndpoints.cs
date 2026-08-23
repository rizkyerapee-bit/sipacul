using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SiPacul.Api.Health;

public static class OperationalHealthEndpoints
{
    private const string ReadinessTag = "ready";

    public static IServiceCollection AddOperationalHealthChecks(
        this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>(
                "database",
                tags: new[] { ReadinessTag });

        return services;
    }

    public static IEndpointRouteBuilder MapOperationalHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapHealthChecks(
                "/health/live",
                CreateOptions(_ => false))
            .AllowAnonymous()
            .ExcludeFromDescription();

        endpoints
            .MapHealthChecks(
                "/health/ready",
                CreateOptions(
                    registration =>
                        registration.Tags.Contains(
                            ReadinessTag)))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static HealthCheckOptions CreateOptions(
        Func<HealthCheckRegistration, bool> predicate)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = WriteResponseAsync
        };
    }

    private static Task WriteResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType =
            "application/json; charset=utf-8";
        context.Response.Headers.CacheControl =
            "no-store, no-cache";

        var response = new OperationalHealthResponse(
            report.Status.ToString(),
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status.ToString()),
            report.TotalDuration.TotalMilliseconds);

        return context.Response.WriteAsJsonAsync(response);
    }

    private sealed record OperationalHealthResponse(
        string Status,
        IReadOnlyDictionary<string, string> Checks,
        double DurationMilliseconds);
}
