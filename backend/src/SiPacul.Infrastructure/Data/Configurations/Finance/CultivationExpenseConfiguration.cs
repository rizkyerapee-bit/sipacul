using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance;

public sealed class CultivationExpenseConfiguration :
    IEntityTypeConfiguration<CultivationExpense>
{
    public void Configure(
        EntityTypeBuilder<CultivationExpense> builder)
    {
        builder.ToTable("CultivationExpenses");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(expense =>
                new
                {
                    expense.OrganizationId,
                    expense.Id
                })
            .HasName(
                "AK_CultivationExpenses_" +
                "OrganizationId_Id");

        builder.Property(expense =>
                expense.OrganizationId)
            .IsRequired();

        builder.Property(expense =>
                expense.CropCycleId)
            .IsRequired();

        builder.Property(expense => expense.Code)
            .HasMaxLength(
                CultivationExpense.MaxCodeLength)
            .IsRequired();

        builder.Property(expense =>
                expense.ExpenseDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(expense => expense.Category)
            .IsRequired();

        builder.Property(expense =>
                expense.Description)
            .HasMaxLength(
                CultivationExpense
                    .MaxDescriptionLength)
            .IsRequired();

        builder.Property(expense => expense.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(expense => expense.PayeeName)
            .HasMaxLength(
                CultivationExpense
                    .MaxPayeeNameLength);

        builder.Property(expense =>
                expense.ReferenceNumber)
            .HasMaxLength(
                CultivationExpense
                    .MaxReferenceNumberLength);

        builder.Property(expense =>
                expense.EvidenceUrl)
            .HasMaxLength(
                CultivationExpense
                    .MaxEvidenceUrlLength);

        builder.Property(expense => expense.Notes)
            .HasMaxLength(
                CultivationExpense.MaxNotesLength);

        builder.Property(expense => expense.Status)
            .IsRequired();

        builder.Property(expense =>
            expense.ConfirmedAt);

        builder.Property(expense =>
                expense.CancellationReason)
            .HasMaxLength(
                CultivationExpense
                    .MaxCancellationReasonLength);

        builder.Ignore(expense =>
            expense.IsRecognizedCost);

        builder.HasIndex(expense =>
                new
                {
                    expense.OrganizationId,
                    expense.CropCycleId,
                    expense.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CultivationExpenses_" +
                "OrganizationId_CropCycleId_Code");

        builder.HasIndex(expense =>
                new
                {
                    expense.OrganizationId,
                    expense.CropCycleId,
                    expense.Status
                })
            .HasDatabaseName(
                "IX_CultivationExpenses_" +
                "OrganizationId_CropCycleId_Status");

        builder.HasIndex(expense =>
                new
                {
                    expense.OrganizationId,
                    expense.ExpenseDate
                })
            .HasDatabaseName(
                "IX_CultivationExpenses_" +
                "OrganizationId_ExpenseDate");

        builder.HasIndex(expense =>
                new
                {
                    expense.OrganizationId,
                    expense.Category
                })
            .HasDatabaseName(
                "IX_CultivationExpenses_" +
                "OrganizationId_Category");

        builder.HasIndex(expense =>
                expense.IsDeleted)
            .HasDatabaseName(
                "IX_CultivationExpenses_IsDeleted");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(expense =>
                expense.OrganizationId)
            .HasConstraintName(
                "FK_CultivationExpenses_" +
                "Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(expense =>
                new
                {
                    expense.OrganizationId,
                    expense.CropCycleId
                })
            .HasPrincipalKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .HasConstraintName(
                "FK_CultivationExpenses_CropCycles_" +
                "OrganizationId_CropCycleId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
