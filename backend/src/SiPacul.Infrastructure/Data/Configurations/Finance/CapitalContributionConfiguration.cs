using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance;

public sealed class CapitalContributionConfiguration :
    IEntityTypeConfiguration<CapitalContribution>
{
    public void Configure(
        EntityTypeBuilder<CapitalContribution> builder)
    {
        builder.ToTable("CapitalContributions");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.Id
                })
            .HasName(
                "AK_CapitalContributions_" +
                "OrganizationId_Id");

        builder.Property(contribution =>
                contribution.OrganizationId)
            .IsRequired();

        builder.Property(contribution =>
                contribution.CropCycleId)
            .IsRequired();

        builder.Property(contribution =>
                contribution.Code)
            .HasMaxLength(
                CapitalContribution.MaxCodeLength)
            .IsRequired();

        builder.Property(contribution =>
                contribution.ContributionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(contribution =>
                contribution.ContributorCode)
            .HasMaxLength(
                CapitalContribution
                    .MaxContributorCodeLength)
            .IsRequired();

        builder.Property(contribution =>
                contribution.ContributorName)
            .HasMaxLength(
                CapitalContribution
                    .MaxContributorNameLength)
            .IsRequired();

        builder.Property(contribution =>
                contribution.ContributorRole)
            .IsRequired();

        builder.Property(contribution =>
                contribution.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(contribution =>
                contribution.PaymentMethod)
            .IsRequired();

        builder.Property(contribution =>
                contribution.ReferenceNumber)
            .HasMaxLength(
                CapitalContribution
                    .MaxReferenceNumberLength);

        builder.Property(contribution =>
                contribution.Notes)
            .HasMaxLength(
                CapitalContribution.MaxNotesLength);

        builder.Property(contribution =>
                contribution.Status)
            .IsRequired();

        builder.Property(contribution =>
            contribution.ConfirmedAt);

        builder.Property(contribution =>
                contribution.CancellationReason)
            .HasMaxLength(
                CapitalContribution
                    .MaxCancellationReasonLength);

        builder.Ignore(contribution =>
            contribution.IsConfirmedCapital);

        builder.Ignore(contribution =>
            contribution.IsInvestorCapital);

        builder.Ignore(contribution =>
            contribution.IsPartnerCapital);

        builder.HasIndex(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.CropCycleId,
                    contribution.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CapitalContributions_" +
                "OrganizationId_CropCycleId_Code");

        builder.HasIndex(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.CropCycleId,
                    contribution.Status
                })
            .HasDatabaseName(
                "IX_CapitalContributions_" +
                "OrganizationId_CropCycleId_Status");

        builder.HasIndex(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.CropCycleId,
                    contribution.ContributorCode
                })
            .HasDatabaseName(
                "IX_CapitalContributions_" +
                "OrganizationId_CropCycleId_" +
                "ContributorCode");

        builder.HasIndex(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.CropCycleId,
                    contribution.ContributorRole,
                    contribution.Status
                })
            .HasDatabaseName(
                "IX_CapitalContributions_" +
                "OrganizationId_CropCycleId_" +
                "ContributorRole_Status");

        builder.HasIndex(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.ContributionDate
                })
            .HasDatabaseName(
                "IX_CapitalContributions_" +
                "OrganizationId_ContributionDate");

        builder.HasIndex(contribution =>
                contribution.IsDeleted)
            .HasDatabaseName(
                "IX_CapitalContributions_IsDeleted");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(contribution =>
                contribution.OrganizationId)
            .HasConstraintName(
                "FK_CapitalContributions_" +
                "Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(contribution =>
                new
                {
                    contribution.OrganizationId,
                    contribution.CropCycleId
                })
            .HasPrincipalKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .HasConstraintName(
                "FK_CapitalContributions_CropCycles_" +
                "OrganizationId_CropCycleId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
