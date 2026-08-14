using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Assignments;

public sealed class
    ProfitSharingSchemeAssignmentResidualShareConfiguration :
    IEntityTypeConfiguration<
        ProfitSharingSchemeAssignmentResidualShare>
{
    public void Configure(
        EntityTypeBuilder<
            ProfitSharingSchemeAssignmentResidualShare> builder)
    {
        builder.ToTable(
            "ProfitSharingSchemeAssignmentResidualShares");

        builder.HasKey(share => share.Id);
        builder.Property(share => share.Id).ValueGeneratedNever();

        builder.Property(share => share.OrganizationId)
            .IsRequired();

        builder.Property(share =>
                share.ProfitSharingSchemeAssignmentId)
            .IsRequired();

        builder.Property(share => share.RecipientCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(share => share.RateNumerator)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(share => share.RateDenominator)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(share => share.Sequence)
            .IsRequired();

        builder.Property(share => share.CreatedAt)
            .IsRequired();

        builder.HasIndex(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingSchemeAssignmentId,
                    share.RecipientCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSAssignmentResidualShares_Assignment_Recipient");

        builder.HasIndex(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingSchemeAssignmentId,
                    share.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSAssignmentResidualShares_Assignment_Sequence");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(share => share.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignmentResidualShares_" +
                "Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
