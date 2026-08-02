using Microsoft.AspNetCore.Mvc;
using SiPacul.Infrastructure.Data;

namespace SiPacul.Api.Common.Http;

public sealed class ProfitSharingSourceLockMiddleware
{
    private readonly RequestDelegate _next;

    public ProfitSharingSourceLockMiddleware(
        RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (
            ProfitSharingSourceLockedException exception)
            when (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode =
                StatusCodes.Status409Conflict;

            context.Response.ContentType =
                "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message,
                Type =
                    "https://httpstatuses.com/409"
            };

            problem.Extensions["code"] =
                exception.ErrorCode;

            problem.Extensions["sourceType"] =
                exception.SourceType;

            problem.Extensions["organizationId"] =
                exception.OrganizationId;

            problem.Extensions["cropCycleId"] =
                exception.CropCycleId;

            problem.Extensions["settlementId"] =
                exception.SettlementId;

            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken:
                    context.RequestAborted);
        }
    }
}
