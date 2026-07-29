using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Cultivation;

public sealed class CultivationSopStepConfiguration :
    IEntityTypeConfiguration<CultivationSopStep>
{
    public void Configure(
        EntityTypeBuilder<CultivationSopStep> builder)
    {
        builder.ToTable("CultivationSopSteps");

        builder.HasKey(step => step.Id);

        builder.Property(step => step.Id)
            .ValueGeneratedNever();

        builder.Property(step => step.OrganizationId)
            .IsRequired();

        builder.Property(step => step.CultivationSopId)
            .IsRequired();

        builder.Property(step => step.Sequence)
            .IsRequired();

        builder.Property(step => step.Name)
            .HasMaxLength(
                CultivationSopStep.MaxNameLength)
            .IsRequired();

        builder.Property(step => step.Description)
            .HasMaxLength(
                CultivationSopStep.MaxDescriptionLength);

        builder.Property(step => step.PlannedDayOffset)
            .IsRequired();

        builder.Property(step =>
                step.EstimatedDurationDays)
            .IsRequired();

        builder.Property(step => step.IsRequired)
            .IsRequired();

        builder.Property(step => step.CreatedAt)
            .IsRequired();

        builder.Property(step => step.UpdatedAt);

        builder.HasIndex(step =>
                new
                {
                    step.OrganizationId,
                    step.CultivationSopId,
                    step.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CultivationSopSteps_" +
                "OrganizationId_SopId_Sequence");

        builder.HasIndex(step =>
                new
                {
                    step.OrganizationId,
                    step.PlannedDayOffset
                })
            .HasDatabaseName(
                "IX_CultivationSopSteps_" +
                "OrganizationId_PlannedDayOffset");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(step =>
                step.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CultivationSop>()
            .WithMany(sop => sop.Steps)
            .HasForeignKey(step =>
                new
                {
                    step.OrganizationId,
                    step.CultivationSopId
                })
            .HasPrincipalKey(sop =>
                new
                {
                    sop.OrganizationId,
                    sop.Id
                })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
