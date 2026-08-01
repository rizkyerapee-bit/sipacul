using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Sales;

public sealed class SaleConfiguration :
    IEntityTypeConfiguration<Sale>
{
    public void Configure(
        EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.Id
                })
            .HasName(
                "AK_Sales_OrganizationId_Id");

        builder.Property(sale => sale.OrganizationId)
            .IsRequired();

        builder.Property(sale => sale.Code)
            .HasMaxLength(Sale.MaxCodeLength)
            .IsRequired();

        builder.Property(sale => sale.SaleDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(sale => sale.BuyerName)
            .HasMaxLength(Sale.MaxBuyerNameLength)
            .IsRequired();

        builder.Property(sale => sale.BuyerPhone)
            .HasMaxLength(Sale.MaxBuyerPhoneLength);

        builder.Property(sale => sale.BuyerAddress)
            .HasMaxLength(Sale.MaxBuyerAddressLength);

        builder.Property(sale => sale.PaymentTerm)
            .IsRequired();

        builder.Property(sale => sale.DueDate)
            .HasColumnType("date");

        builder.Property(sale => sale.DiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sale => sale.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sale => sale.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sale => sale.Status)
            .IsRequired()
            .HasDefaultValue(SaleStatus.Draft);

        builder.Property(sale => sale.ConfirmedAt);

        builder.Property(sale =>
                sale.CancellationReason)
            .HasMaxLength(
                Sale.MaxCancellationReasonLength);

        builder.Property(sale => sale.Notes)
            .HasMaxLength(Sale.MaxNotesLength);

        builder.Ignore(sale => sale.IsRevenue);

        builder.HasIndex(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_Sales_OrganizationId_Code");

        builder.HasIndex(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.SaleDate
                })
            .HasDatabaseName(
                "IX_Sales_OrganizationId_SaleDate");

        builder.HasIndex(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.Status
                })
            .HasDatabaseName(
                "IX_Sales_OrganizationId_Status");

        builder.HasIndex(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.BuyerName
                })
            .HasDatabaseName(
                "IX_Sales_OrganizationId_BuyerName");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(sale =>
                sale.OrganizationId)
            .HasConstraintName(
                "FK_Sales_Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sale => sale.Lines)
            .WithOne()
            .HasForeignKey(line =>
                new
                {
                    line.OrganizationId,
                    line.SaleId
                })
            .HasPrincipalKey(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.Id
                })
            .HasConstraintName(
                "FK_SaleLines_Sales_" +
                "OrganizationId_SaleId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(sale => sale.Lines)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
