using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Assignments;

public sealed class ProfitSharingSchemeAssignmentConfiguration :
    IEntityTypeConfiguration<ProfitSharingSchemeAssignment>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingSchemeAssignment> builder)
    {
        builder.ToTable("ProfitSharingSchemeAssignments");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.Id
                })
            .HasName(
                "AK_ProfitSharingSchemeAssignments_Org_Id");

        builder.Property(assignment => assignment.OrganizationId)
            .IsRequired();

        builder.Property(assignment => assignment.CropCycleId)
            .IsRequired();

        builder.Property(assignment => assignment.SourceSchemeId)
            .IsRequired();

        builder.Property(assignment => assignment.SchemeFamilyId)
            .IsRequired();

        builder.Property(assignment => assignment.SchemeCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(assignment => assignment.SchemeName)
            .HasMaxLength(ProfitSharingScheme.MaxNameLength)
            .IsRequired();

        builder.Property(assignment => assignment.SchemeDescription)
            .HasMaxLength(ProfitSharingScheme.MaxDescriptionLength);

        builder.Property(assignment => assignment.SchemeVersion)
            .IsRequired();

        builder.Property(assignment => assignment.ResidualMethod)
            .IsRequired();

        builder.Property(assignment =>
                assignment.ResidualRecipientCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength);

        builder.Property(assignment => assignment.AssignedAt)
            .IsRequired();

        builder.HasIndex(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.CropCycleId
                })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName(
                "UX_ProfitSharingSchemeAssignments_Org_Cycle");

        builder.HasIndex(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.SourceSchemeId
                })
            .HasDatabaseName(
                "IX_ProfitSharingSchemeAssignments_Org_Scheme");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(assignment => assignment.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignments_Organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.CropCycleId
                })
            .HasPrincipalKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignments_CropCycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProfitSharingScheme>()
            .WithMany()
            .HasForeignKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.SourceSchemeId
                })
            .HasPrincipalKey(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignments_SourceScheme")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(assignment => assignment.Participants)
            .WithOne()
            .HasForeignKey(participant =>
                new
                {
                    participant.OrganizationId,
                    participant.ProfitSharingSchemeAssignmentId
                })
            .HasPrincipalKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignmentParticipants_" +
                "Assignment")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(assignment => assignment.PriorityRules)
            .WithOne()
            .HasForeignKey(rule =>
                new
                {
                    rule.OrganizationId,
                    rule.ProfitSharingSchemeAssignmentId
                })
            .HasPrincipalKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignmentPriorityRules_" +
                "Assignment")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(assignment => assignment.ResidualShares)
            .WithOne()
            .HasForeignKey(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingSchemeAssignmentId
                })
            .HasPrincipalKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignmentResidualShares_" +
                "Assignment")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(assignment => assignment.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(assignment => assignment.PriorityRules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(assignment => assignment.ResidualShares)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
