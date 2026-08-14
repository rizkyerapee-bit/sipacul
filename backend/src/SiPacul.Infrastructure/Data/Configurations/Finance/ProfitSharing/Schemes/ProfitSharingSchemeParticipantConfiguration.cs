using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemeParticipantConfiguration :
    IEntityTypeConfiguration<ProfitSharingSchemeParticipant>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingSchemeParticipant> builder)
    {
        builder.ToTable("ProfitSharingSchemeParticipants");

        builder.HasKey(participant => participant.Id);
        builder.Property(participant => participant.Id)
            .ValueGeneratedNever();

        builder.Property(participant => participant.OrganizationId)
            .IsRequired();

        builder.Property(participant =>
                participant.ProfitSharingSchemeId)
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
                    participant.ProfitSharingSchemeId,
                    participant.ParticipantCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemeParticipants_Scheme_Code");

        builder.HasIndex(participant =>
                new
                {
                    participant.OrganizationId,
                    participant.ProfitSharingSchemeId,
                    participant.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemeParticipants_Scheme_Sequence");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(participant => participant.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemeParticipants_Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
