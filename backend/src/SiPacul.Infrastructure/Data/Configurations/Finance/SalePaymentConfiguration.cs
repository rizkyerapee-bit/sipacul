using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance;

public sealed class SalePaymentConfiguration :
    IEntityTypeConfiguration<SalePayment>
{
    public void Configure(
        EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayments");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.Id
                })
            .HasName(
                "AK_SalePayments_OrganizationId_Id");

        builder.Property(payment =>
                payment.OrganizationId)
            .IsRequired();

        builder.Property(payment =>
                payment.SaleId)
            .IsRequired();

        builder.Property(payment =>
                payment.Code)
            .HasMaxLength(
                SalePayment.MaxCodeLength)
            .IsRequired();

        builder.Property(payment =>
                payment.PaymentDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(payment =>
                payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment =>
                payment.PaymentMethod)
            .IsRequired();

        builder.Property(payment =>
                payment.ReferenceNumber)
            .HasMaxLength(
                SalePayment
                    .MaxReferenceNumberLength);

        builder.Property(payment =>
                payment.ReceivedFrom)
            .HasMaxLength(
                SalePayment.MaxReceivedFromLength);

        builder.Property(payment =>
                payment.Notes)
            .HasMaxLength(
                SalePayment.MaxNotesLength);

        builder.Property(payment =>
                payment.Status)
            .IsRequired();

        builder.Property(payment =>
            payment.ConfirmedAt);

        builder.Property(payment =>
                payment.CancellationReason)
            .HasMaxLength(
                SalePayment
                    .MaxCancellationReasonLength);

        builder.Ignore(payment =>
            payment.IsCollectedRevenue);

        builder.HasIndex(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_SalePayments_OrganizationId_Code");

        builder.HasIndex(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.SaleId,
                    payment.Status
                })
            .HasDatabaseName(
                "IX_SalePayments_" +
                "OrganizationId_SaleId_Status");

        builder.HasIndex(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.SaleId,
                    payment.PaymentDate
                })
            .HasDatabaseName(
                "IX_SalePayments_" +
                "OrganizationId_SaleId_PaymentDate");

        builder.HasIndex(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.Status
                })
            .HasDatabaseName(
                "IX_SalePayments_" +
                "OrganizationId_Status");

        builder.HasIndex(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.PaymentDate
                })
            .HasDatabaseName(
                "IX_SalePayments_" +
                "OrganizationId_PaymentDate");

        builder.HasIndex(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.ReceivedFrom
                })
            .HasDatabaseName(
                "IX_SalePayments_" +
                "OrganizationId_ReceivedFrom");

        builder.HasIndex(payment =>
                payment.IsDeleted)
            .HasDatabaseName(
                "IX_SalePayments_IsDeleted");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(payment =>
                payment.OrganizationId)
            .HasConstraintName(
                "FK_SalePayments_Organizations_" +
                "OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sale>()
            .WithMany()
            .HasForeignKey(payment =>
                new
                {
                    payment.OrganizationId,
                    payment.SaleId
                })
            .HasPrincipalKey(sale =>
                new
                {
                    sale.OrganizationId,
                    sale.Id
                })
            .HasConstraintName(
                "FK_SalePayments_Sales_" +
                "OrganizationId_SaleId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
