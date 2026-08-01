using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance;

public sealed class SalePaymentConfigurationTests
{
    [Fact]
    public void Payment_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SalePayment));

        Assert.NotNull(entityType);

        Assert.Equal(
            "SalePayments",
            entityType!.GetTableName());

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_SalePayments_" +
                        "OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(SalePayment.OrganizationId),
                nameof(SalePayment.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void PaymentDate_ShouldUsePostgreSqlDate()
    {
        using var context = CreateContext();

        var property =
            context.Model
                .FindEntityType(
                    typeof(SalePayment))!
                .FindProperty(
                    nameof(
                        SalePayment.PaymentDate))!;

        Assert.Equal(
            "date",
            property.GetColumnType());

        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Amount_ShouldUseMoneyPrecision()
    {
        using var context = CreateContext();

        var property =
            context.Model
                .FindEntityType(
                    typeof(SalePayment))!
                .FindProperty(
                    nameof(SalePayment.Amount))!;

        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(2, property.GetScale());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Payment_ShouldUseExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SalePayment))!;

        AssertLength(
            entityType,
            nameof(SalePayment.Code),
            SalePayment.MaxCodeLength);

        AssertLength(
            entityType,
            nameof(SalePayment.ReferenceNumber),
            SalePayment.MaxReferenceNumberLength);

        AssertLength(
            entityType,
            nameof(SalePayment.ReceivedFrom),
            SalePayment.MaxReceivedFromLength);

        AssertLength(
            entityType,
            nameof(SalePayment.Notes),
            SalePayment.MaxNotesLength);

        AssertLength(
            entityType,
            nameof(
                SalePayment.CancellationReason),
            SalePayment.MaxCancellationReasonLength);
    }

    [Fact]
    public void MethodAndStatus_ShouldBeRequired()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SalePayment))!;

        Assert.False(
            entityType.FindProperty(
                nameof(
                    SalePayment.PaymentMethod))!
                .IsNullable);

        Assert.False(
            entityType.FindProperty(
                nameof(SalePayment.Status))!
                .IsNullable);
    }

    [Fact]
    public void CollectedRevenueFlag_ShouldNotBeMapped()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SalePayment))!;

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    SalePayment.IsCollectedRevenue)));
    }

    [Fact]
    public void Payment_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SalePayment))!;

        var indexes =
            entityType.GetIndexes()
                .ToDictionary(
                    index =>
                        index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_SalePayments_" +
                "OrganizationId_Code"]
                .IsUnique);

        Assert.Contains(
            "IX_SalePayments_" +
            "OrganizationId_SaleId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_SalePayments_" +
            "OrganizationId_SaleId_PaymentDate",
            indexes.Keys);

        Assert.Contains(
            "IX_SalePayments_" +
            "OrganizationId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_SalePayments_" +
            "OrganizationId_PaymentDate",
            indexes.Keys);

        Assert.Contains(
            "IX_SalePayments_" +
            "OrganizationId_ReceivedFrom",
            indexes.Keys);

        Assert.Contains(
            "IX_SalePayments_IsDeleted",
            indexes.Keys);
    }

    [Fact]
    public void Payment_ShouldReferenceOrganizationRestrictively()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Organization),
            new[]
            {
                nameof(SalePayment.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            });
    }

    [Fact]
    public void Payment_ShouldUseScopedSaleForeignKey()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Sale),
            new[]
            {
                nameof(SalePayment.OrganizationId),
                nameof(SalePayment.SaleId)
            },
            new[]
            {
                nameof(Sale.OrganizationId),
                nameof(Sale.Id)
            });
    }

    [Fact]
    public void DbContext_ShouldExposeSalePayments()
    {
        using var context = CreateContext();

        Assert.NotNull(context.SalePayments);
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
                typeof(SalePayment));

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
