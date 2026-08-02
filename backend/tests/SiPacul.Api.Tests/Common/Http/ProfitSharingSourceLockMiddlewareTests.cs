using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SiPacul.Api.Common.Http;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Api.Tests.Common.Http;

public sealed class
    ProfitSharingSourceLockMiddlewareTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid SettlementId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    [Theory]
    [InlineData(
        "CultivationExpenses.FinalizedSettlementExists",
        "cultivation expense")]
    [InlineData(
        "CapitalContributions.FinalizedSettlementExists",
        "capital contribution")]
    [InlineData(
        "SalePayments.FinalizedSettlementExists",
        "sale payment")]
    public async Task Middleware_ShouldMapSourceLockToConflict(
        string errorCode,
        string sourceType)
    {
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();

        var exception =
            new ProfitSharingSourceLockedException(
                errorCode,
                sourceType,
                OrganizationId,
                CropCycleId,
                SettlementId);

        var middleware =
            new ProfitSharingSourceLockMiddleware(
                _ => throw exception);

        await middleware.InvokeAsync(context);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            context.Response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            context.Response.ContentType);

        context.Response.Body.Position = 0;

        using var document =
            await JsonDocument.ParseAsync(
                context.Response.Body);

        var root = document.RootElement;

        Assert.Equal(
            StatusCodes.Status409Conflict,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            errorCode,
            root.GetProperty("code").GetString());

        Assert.Equal(
            sourceType,
            root.GetProperty("sourceType").GetString());

        Assert.Equal(
            CropCycleId.ToString(),
            root.GetProperty("cropCycleId")
                .GetString());
    }

    [Fact]
    public async Task Middleware_WithoutException_ShouldContinue()
    {
        var context = new DefaultHttpContext();

        var called = false;

        var middleware =
            new ProfitSharingSourceLockMiddleware(
                nextContext =>
                {
                    called = true;

                    nextContext.Response.StatusCode =
                        StatusCodes.Status204NoContent;

                    return Task.CompletedTask;
                });

        await middleware.InvokeAsync(context);

        Assert.True(called);

        Assert.Equal(
            StatusCodes.Status204NoContent,
            context.Response.StatusCode);
    }
}
