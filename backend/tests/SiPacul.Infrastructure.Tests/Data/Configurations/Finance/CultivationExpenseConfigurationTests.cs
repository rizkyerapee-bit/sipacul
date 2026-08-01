using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance;

public sealed class CultivationExpenseConfigurationTests
{
    [Fact]
    public void Expense_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CultivationExpense));

        Assert.NotNull(entityType);

        Assert.Equal(
            "CultivationExpenses",
            entityType!.GetTableName());

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_CultivationExpenses_" +
                        "OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(
                    CultivationExpense.OrganizationId),
                nameof(CultivationExpense.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void ExpenseDate_ShouldUsePostgreSqlDate()
    {
        using var context = CreateContext();

        var property =
            context.Model
                .FindEntityType(
                    typeof(CultivationExpense))!
                .FindProperty(
                    nameof(
                        CultivationExpense.ExpenseDate))!;

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
                    typeof(CultivationExpense))!
                .FindProperty(
                    nameof(CultivationExpense.Amount))!;

        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(2, property.GetScale());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Expense_ShouldUseExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CultivationExpense))!;

        AssertLength(
            entityType,
            nameof(CultivationExpense.Code),
            CultivationExpense.MaxCodeLength);

        AssertLength(
            entityType,
            nameof(CultivationExpense.Description),
            CultivationExpense.MaxDescriptionLength);

        AssertLength(
            entityType,
            nameof(CultivationExpense.PayeeName),
            CultivationExpense.MaxPayeeNameLength);

        AssertLength(
            entityType,
            nameof(CultivationExpense.ReferenceNumber),
            CultivationExpense.MaxReferenceNumberLength);

        AssertLength(
            entityType,
            nameof(CultivationExpense.EvidenceUrl),
            CultivationExpense.MaxEvidenceUrlLength);

        AssertLength(
            entityType,
            nameof(
                CultivationExpense.CancellationReason),
            CultivationExpense
                .MaxCancellationReasonLength);

        AssertLength(
            entityType,
            nameof(CultivationExpense.Notes),
            CultivationExpense.MaxNotesLength);
    }

    [Fact]
    public void CategoryAndStatus_ShouldBeRequired()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CultivationExpense))!;

        Assert.False(
            entityType.FindProperty(
                nameof(CultivationExpense.Category))!
                .IsNullable);

        Assert.False(
            entityType.FindProperty(
                nameof(CultivationExpense.Status))!
                .IsNullable);
    }

    [Fact]
    public void RecognizedCost_ShouldNotBeMapped()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CultivationExpense))!;

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    CultivationExpense
                        .IsRecognizedCost)));
    }

    [Fact]
    public void Expense_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(CultivationExpense))!;

        var indexes =
            entityType.GetIndexes()
                .ToDictionary(
                    index =>
                        index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_CultivationExpenses_" +
                "OrganizationId_CropCycleId_Code"]
                .IsUnique);

        Assert.Contains(
            "IX_CultivationExpenses_" +
            "OrganizationId_CropCycleId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_CultivationExpenses_" +
            "OrganizationId_ExpenseDate",
            indexes.Keys);

        Assert.Contains(
            "IX_CultivationExpenses_" +
            "OrganizationId_Category",
            indexes.Keys);

        Assert.Contains(
            "IX_CultivationExpenses_IsDeleted",
            indexes.Keys);
    }

    [Fact]
    public void Expense_ShouldReferenceOrganizationRestrictively()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Organization),
            new[]
            {
                nameof(
                    CultivationExpense.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            });
    }

    [Fact]
    public void Expense_ShouldUseScopedCropCycleForeignKey()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CropCycle),
            new[]
            {
                nameof(
                    CultivationExpense.OrganizationId),
                nameof(
                    CultivationExpense.CropCycleId)
            },
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.Id)
            });
    }

    [Fact]
    public void DbContext_ShouldExposeCultivationExpenses()
    {
        using var context = CreateContext();

        Assert.NotNull(
            context.CultivationExpenses);
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
                typeof(CultivationExpense));

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
