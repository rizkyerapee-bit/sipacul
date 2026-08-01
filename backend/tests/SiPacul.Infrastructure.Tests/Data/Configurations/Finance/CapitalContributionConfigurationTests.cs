using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance;

public sealed class CapitalContributionConfigurationTests
{
    [Fact]
    public void Contribution_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CapitalContribution));

        Assert.NotNull(entityType);

        Assert.Equal(
            "CapitalContributions",
            entityType!.GetTableName());

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_CapitalContributions_" +
                        "OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(
                    CapitalContribution.OrganizationId),
                nameof(CapitalContribution.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void ContributionDate_ShouldUsePostgreSqlDate()
    {
        using var context = CreateContext();

        var property =
            context.Model
                .FindEntityType(
                    typeof(CapitalContribution))!
                .FindProperty(
                    nameof(
                        CapitalContribution
                            .ContributionDate))!;

        Assert.Equal(
            "date",
            property.GetColumnType());
    }

    [Fact]
    public void Amount_ShouldUseMoneyPrecision()
    {
        using var context = CreateContext();

        var property =
            context.Model
                .FindEntityType(
                    typeof(CapitalContribution))!
                .FindProperty(
                    nameof(
                        CapitalContribution.Amount))!;

        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(2, property.GetScale());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Contribution_ShouldUseExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CapitalContribution))!;

        AssertLength(
            entityType,
            nameof(CapitalContribution.Code),
            CapitalContribution.MaxCodeLength);

        AssertLength(
            entityType,
            nameof(
                CapitalContribution.ContributorCode),
            CapitalContribution
                .MaxContributorCodeLength);

        AssertLength(
            entityType,
            nameof(
                CapitalContribution.ContributorName),
            CapitalContribution
                .MaxContributorNameLength);

        AssertLength(
            entityType,
            nameof(
                CapitalContribution.ReferenceNumber),
            CapitalContribution
                .MaxReferenceNumberLength);

        AssertLength(
            entityType,
            nameof(
                CapitalContribution
                    .CancellationReason),
            CapitalContribution
                .MaxCancellationReasonLength);

        AssertLength(
            entityType,
            nameof(CapitalContribution.Notes),
            CapitalContribution.MaxNotesLength);
    }

    [Fact]
    public void RolePaymentMethodAndStatus_ShouldBeRequired()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CapitalContribution))!;

        Assert.False(
            entityType.FindProperty(
                nameof(
                    CapitalContribution
                        .ContributorRole))!
                .IsNullable);

        Assert.False(
            entityType.FindProperty(
                nameof(
                    CapitalContribution
                        .PaymentMethod))!
                .IsNullable);

        Assert.False(
            entityType.FindProperty(
                nameof(
                    CapitalContribution.Status))!
                .IsNullable);
    }

    [Fact]
    public void ComputedCapitalFlags_ShouldNotBeMapped()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CapitalContribution))!;

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    CapitalContribution
                        .IsConfirmedCapital)));

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    CapitalContribution
                        .IsInvestorCapital)));

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    CapitalContribution
                        .IsPartnerCapital)));
    }

    [Fact]
    public void Contribution_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CapitalContribution))!;

        var indexes =
            entityType.GetIndexes()
                .ToDictionary(
                    index =>
                        index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_CapitalContributions_" +
                "OrganizationId_CropCycleId_Code"]
                .IsUnique);

        Assert.Contains(
            "IX_CapitalContributions_" +
            "OrganizationId_CropCycleId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_CapitalContributions_" +
            "OrganizationId_CropCycleId_" +
            "ContributorCode",
            indexes.Keys);

        Assert.Contains(
            "IX_CapitalContributions_" +
            "OrganizationId_CropCycleId_" +
            "ContributorRole_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_CapitalContributions_" +
            "OrganizationId_ContributionDate",
            indexes.Keys);

        Assert.Contains(
            "IX_CapitalContributions_IsDeleted",
            indexes.Keys);
    }

    [Fact]
    public void Contribution_ShouldReferenceOrganizationRestrictively()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Organization),
            new[]
            {
                nameof(
                    CapitalContribution.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            });
    }

    [Fact]
    public void Contribution_ShouldUseScopedCropCycleForeignKey()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CropCycle),
            new[]
            {
                nameof(
                    CapitalContribution.OrganizationId),
                nameof(
                    CapitalContribution.CropCycleId)
            },
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.Id)
            });
    }

    [Fact]
    public void DbContext_ShouldExposeCapitalContributions()
    {
        using var context = CreateContext();

        Assert.NotNull(
            context.CapitalContributions);
    }

    private static void AssertLength(
        IEntityType entityType,
        string propertyName,
        int expectedLength)
    {
        var property =
            entityType.FindProperty(propertyName);

        Assert.NotNull(property);

        Assert.Equal(
            expectedLength,
            property!.GetMaxLength());
    }

    private static void AssertForeignKey(
        SiPaculDbContext context,
        Type principalType,
        string[] expectedForeignKeyProperties,
        string[] expectedPrincipalKeyProperties)
    {
        var entityType =
            context.Model.FindEntityType(
                typeof(CapitalContribution));

        Assert.NotNull(entityType);

        var foreignKey =
            entityType!.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                        principalType);

        Assert.Equal(
            expectedForeignKeyProperties,
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            expectedPrincipalKeyProperties,
            foreignKey.PrincipalKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);
    }

    private static SiPaculDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Port=5432;" +
                    "Database=sipacul_model_tests;" +
                    "Username=sipacul;" +
                    "Password=sipacul")
                .Options;

        return new SiPaculDbContext(options);
    }
}
