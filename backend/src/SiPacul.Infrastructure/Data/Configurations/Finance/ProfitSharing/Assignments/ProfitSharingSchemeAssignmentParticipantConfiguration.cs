using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Assignments;

public sealed class
    ProfitSharingSchemeAssignmentParticipantConfiguration :
    IEntityTypeConfiguration<
        ProfitSharingSchemeAssignmentParticipant>
{
    public void Configure(
        EntityTypeBuilder<
            ProfitSharingSchemeAssignmentParticipant> builder)
    {
        builder.ToTable(
            "ProfitSharingSchemeAssignmentParticipants");

        builder.HasKey(participant => participant.Id);
        builder.Property(participant => participant.Id)
            .ValueGeneratedNever();

        builder.Property(participant => participant.OrganizationId)
            .IsRequired();

        builder.Property(participant =>
                participant.ProfitSharingSchemeAssignmentId)
            .IsRequired();

        builder.Property(participant => participant.ParticipantCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(participant => participant.ParticipantName)
            .HasMaxLength(
                ProfitSharingScheme.MaxParticipantNameLength)
            .IsRequired();

        builder.Property(participant => participant.ParticipantRole)
            .IsRequired();

        builder.Property(participant =>
                participant.ParticipatesInResidualProfit)
            .IsRequired();

        builder.Property(participant => participant.Sequence)
            .IsRequired();

        builder.Property(participant => participant.CreatedAt)
            .IsRequired();

        builder.HasIndex(participant =>
                new
                {
                    participant.OrganizationId,
                    participant.ProfitSharingSchemeAssignmentId,
                    participant.ParticipantCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSAssignmentParticipants_Assignment_Code");

        builder.HasIndex(participant =>
                new
                {
                    participant.OrganizationId,
                    participant.ProfitSharingSchemeAssignmentId,
                    participant.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSAssignmentParticipants_Assignment_Sequence");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(participant => participant.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignmentParticipants_" +
                "Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
