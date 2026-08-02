using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Infrastructure.Data.Configurations.Organizations;

public sealed class OrganizationMembershipConfiguration :
    IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(
        EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("OrganizationMemberships");

        builder.ConfigureAuditableEntity();

        builder.Property(membership =>
                membership.OrganizationId)
            .IsRequired();

        builder.Property(membership =>
                membership.UserId)
            .IsRequired();

        builder.Property(membership => membership.Role)
            .IsRequired();

        builder.Property(membership => membership.Status)
            .IsRequired();

        builder.Property(membership =>
                membership.JoinedAt)
            .IsRequired();

        builder.Property(membership =>
                membership.SuspendedAt);

        builder.HasAlternateKey(membership =>
                new
                {
                    membership.OrganizationId,
                    membership.Id
                })
            .HasName(
                "AK_OrganizationMemberships_Org_Id");

        builder.HasIndex(membership =>
                new
                {
                    membership.OrganizationId,
                    membership.UserId
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_OrganizationMemberships_Org_User");

        builder.HasIndex(membership =>
                new
                {
                    membership.OrganizationId,
                    membership.Status,
                    membership.IsDeleted
                })
            .HasDatabaseName(
                "IX_OrganizationMemberships_" +
                "Org_Status");

        builder.HasIndex(membership =>
                new
                {
                    membership.UserId,
                    membership.Status,
                    membership.IsDeleted
                })
            .HasDatabaseName(
                "IX_OrganizationMemberships_" +
                "User_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(membership =>
                membership.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "FK_OrganizationMemberships_" +
                "Organization");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership =>
                membership.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "FK_OrganizationMemberships_User");
    }
}
