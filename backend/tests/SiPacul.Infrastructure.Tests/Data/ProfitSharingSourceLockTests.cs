using System.Reflection;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data;

public sealed class ProfitSharingSourceLockTests
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

    [Fact]
    public void Exception_ShouldPreserveLockMetadata()
    {
        var exception =
            new ProfitSharingSourceLockedException(
                "CultivationExpenses." +
                "FinalizedSettlementExists",
                "cultivation expense",
                OrganizationId,
                CropCycleId,
                SettlementId);

        Assert.Equal(
            "CultivationExpenses." +
            "FinalizedSettlementExists",
            exception.ErrorCode);

        Assert.Equal(
            "cultivation expense",
            exception.SourceType);

        Assert.Equal(
            OrganizationId,
            exception.OrganizationId);

        Assert.Equal(
            CropCycleId,
            exception.CropCycleId);

        Assert.Equal(
            SettlementId,
            exception.SettlementId);

        Assert.Contains(
            SettlementId.ToString(),
            exception.Message);
    }

    [Fact]
    public void DbContext_ShouldDeclareSyncAndAsyncSaveOverrides()
    {
        var declaredMethods =
            typeof(SiPaculDbContext)
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly)
                .Where(method =>
                    method.Name is
                        nameof(SiPaculDbContext.SaveChanges) or
                        nameof(
                            SiPaculDbContext.SaveChangesAsync))
                .ToArray();

        Assert.Contains(
            declaredMethods,
            method =>
                method.Name ==
                    nameof(
                        SiPaculDbContext.SaveChanges) &&
                method.GetParameters().Length == 1);

        Assert.Contains(
            declaredMethods,
            method =>
                method.Name ==
                    nameof(
                        SiPaculDbContext.SaveChangesAsync) &&
                method.GetParameters().Length == 1);

        Assert.Contains(
            declaredMethods,
            method =>
                method.Name ==
                    nameof(
                        SiPaculDbContext.SaveChangesAsync) &&
                method.GetParameters().Length == 2);
    }
}
