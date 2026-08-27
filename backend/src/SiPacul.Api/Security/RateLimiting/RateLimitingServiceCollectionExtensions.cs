using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SiPacul.Api.Security.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddSiPaculRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter =
                PartitionedRateLimiter.Create<
                    HttpContext,
                    string>(CreateGlobalPartition);

            options.AddPolicy(
                SiPaculRateLimitingDefaults
                    .AuthenticationPolicyName,
                context => CreateClientPartition(
                    context,
                    "authentication",
                    SiPaculRateLimitingDefaults
                        .AuthenticationPermitLimit));

            options.AddPolicy(
                SiPaculRateLimitingDefaults
                    .BootstrapPolicyName,
                context => CreateClientPartition(
                    context,
                    "bootstrap",
                    SiPaculRateLimitingDefaults
                        .BootstrapPermitLimit));

            options.OnRejected = WriteRejectionAsync;
        });

        return services;
    }

    private static RateLimitPartition<string>
        CreateGlobalPartition(
            HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(
                "/health"))
        {
            return RateLimitPartition.GetNoLimiter(
                "health");
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            $"global:{GetUserOrClientKey(context)}",
            _ => CreateFixedWindowOptions(
                SiPaculRateLimitingDefaults
                    .GlobalPermitLimit));
    }

    private static RateLimitPartition<string>
        CreateClientPartition(
            HttpContext context,
            string scope,
            int permitLimit)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            $"{scope}:{GetClientKey(context)}",
            _ => CreateFixedWindowOptions(
                permitLimit));
    }

    private static FixedWindowRateLimiterOptions
        CreateFixedWindowOptions(
            int permitLimit)
    {
        return new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst,
            Window = SiPaculRateLimitingDefaults.Window
        };
    }

    private static string GetUserOrClientKey(
        HttpContext context)
    {
        var userId =
            context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(userId)
            ? GetClientKey(context)
            : $"user:{userId}";
    }

    private static string GetClientKey(
        HttpContext context)
    {
        var address =
            context.Connection.RemoteIpAddress
                ?.ToString() ??
            "unknown";

        return $"ip:{address}";
    }

    private static async ValueTask WriteRejectionAsync(
        OnRejectedContext context,
        CancellationToken _)
    {
        var response = context.HttpContext.Response;

        response.Headers["Cache-Control"] =
            "no-store";

        response.Headers["Pragma"] =
            "no-cache";

        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            response.Headers["Retry-After"] =
                Math.Ceiling(
                        retryAfter.TotalSeconds)
                    .ToString(
                        CultureInfo.InvariantCulture);
        }

        var problem = Results.Problem(
            statusCode:
                StatusCodes.Status429TooManyRequests,
            title:
                "Too many requests",
            detail:
                "Terlalu banyak permintaan. " +
                "Tunggu sebelum mencoba kembali.",
            extensions:
                new Dictionary<string, object?>
                {
                    ["code"] =
                        SiPaculRateLimitingDefaults
                            .RejectionCode
                });

        await problem.ExecuteAsync(
            context.HttpContext);
    }
}
