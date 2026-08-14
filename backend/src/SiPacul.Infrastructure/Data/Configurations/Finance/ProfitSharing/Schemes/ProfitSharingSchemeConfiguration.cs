using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemeConfiguration :
    IEntityTypeConfiguration<ProfitSharingScheme>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingScheme> builder)
    {
        builder.ToTable("ProfitSharingSchemes");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Id
                })
            .HasName("AK_ProfitSharingSchemes_Org_Id");

        builder.Property(scheme => scheme.OrganizationId)
            .IsRequired();

        builder.Property(scheme => scheme.SchemeFamilyId)
            .IsRequired();

        builder.Property(scheme => scheme.Code)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(scheme => scheme.Name)
            .HasMaxLength(ProfitSharingScheme.MaxNameLength)
            .IsRequired();

        builder.Property(scheme => scheme.Description)
            .HasMaxLength(
                ProfitSharingScheme.MaxDescriptionLength);

        builder.Property(scheme => scheme.Version)
            .IsRequired();

        builder.Property(scheme => scheme.Status)
            .IsRequired();

        builder.Property(scheme => scheme.ResidualMethod)
            .IsRequired();

        builder.Property(scheme => scheme.ResidualRecipientCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength);

        builder.Property(scheme => scheme.ActivatedAt);
        builder.Property(scheme => scheme.SupersededAt);

        builder.HasIndex(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.SchemeFamilyId,
                    scheme.Version
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemes_Org_Family_Version");

        builder.HasIndex(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Code,
                    scheme.Version
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemes_Org_Code_Version");

        builder.HasIndex(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.SchemeFamilyId,
                    scheme.Status
                })
            .IsUnique()
            .HasFilter(
                "\"Status\" IN (1, 2) AND " +
                "\"IsDeleted\" = false")
            .HasDatabaseName(
                "UX_ProfitSharingSchemes_Org_Family_OpenStatus");

        builder.HasIndex(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Status
                })
            .HasDatabaseName(
                "IX_ProfitSharingSchemes_Org_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(scheme => scheme.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemes_Organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(scheme => scheme.Participants)
            .WithOne()
            .HasForeignKey(participant =>
                new
                {
                    participant.OrganizationId,
                    participant.ProfitSharingSchemeId
                })
            .HasPrincipalKey(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeParticipants_Scheme")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(scheme => scheme.PriorityRules)
            .WithOne()
            .HasForeignKey(rule =>
                new
                {
                    rule.OrganizationId,
                    rule.ProfitSharingSchemeId
                })
            .HasPrincipalKey(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemePriorityRules_Scheme")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(scheme => scheme.ResidualShares)
            .WithOne()
            .HasForeignKey(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingSchemeId
                })
            .HasPrincipalKey(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSchemeResidualShares_Scheme")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(scheme => scheme.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(scheme => scheme.PriorityRules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(scheme => scheme.ResidualShares)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
