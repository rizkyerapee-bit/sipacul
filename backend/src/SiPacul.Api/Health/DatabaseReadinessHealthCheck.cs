using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SiPacul.Infrastructure.Data;

namespace SiPacul.Api.Health;

public sealed class DatabaseReadinessHealthCheck(
    SiPaculDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect =
                await dbContext.Database.CanConnectAsync(
                    cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy(
                    "PostgreSQL connection is available.")
                : HealthCheckResult.Unhealthy(
                    "PostgreSQL connection is unavailable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL readiness check failed.",
                exception);
        }
    }
}
