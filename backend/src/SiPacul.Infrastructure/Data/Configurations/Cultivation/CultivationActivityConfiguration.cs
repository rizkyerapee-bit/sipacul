using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Cultivation;

public sealed class CultivationActivityConfiguration :
    IEntityTypeConfiguration<CultivationActivity>
{
    public void Configure(
        EntityTypeBuilder<CultivationActivity> builder)
    {
        builder.ToTable("CultivationActivities");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.Id
                })
            .HasName(
                "AK_CultivationActivities_" +
                "OrganizationId_Id");

        builder.Property(activity =>
                activity.OrganizationId)
            .IsRequired();

        builder.Property(activity =>
                activity.CropCycleId)
            .IsRequired();

        builder.Property(activity => activity.Code)
            .HasMaxLength(
                CultivationActivity.MaxCodeLength)
            .IsRequired();

        builder.Property(activity => activity.Name)
            .HasMaxLength(
                CultivationActivity.MaxNameLength)
            .IsRequired();

        builder.Property(activity =>
                activity.ActivityType)
            .IsRequired();

        builder.Property(activity =>
            activity.CultivationSopId);

        builder.Property(activity =>
            activity.CultivationSopStepId);

        builder.Property(activity =>
            activity.SopStepSequenceSnapshot);

        builder.Property(activity =>
                activity.SopStepNameSnapshot)
            .HasMaxLength(
                CultivationActivity
                    .MaxSopStepNameLength);

        builder.Property(activity =>
            activity.SopPlannedDayOffsetSnapshot);

        builder.Property(activity =>
            activity.SopEstimatedDurationDaysSnapshot);

        builder.Property(activity =>
            activity.SopIsRequiredSnapshot);

        builder.Property(activity =>
                activity.PlannedDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(activity =>
                activity.ActualStartDate)
            .HasColumnType("date");

        builder.Property(activity =>
                activity.ActualCompletionDate)
            .HasColumnType("date");

        builder.Property(activity => activity.Status)
            .IsRequired()
            .HasDefaultValue(
                CultivationActivityStatus.Planned);

        builder.Property(activity =>
                activity.SopComplianceStatus)
            .IsRequired();

        builder.Property(activity => activity.Outcome)
            .HasMaxLength(
                CultivationActivity.MaxOutcomeLength);

        builder.Property(activity =>
                activity.IssueNotes)
            .HasMaxLength(
                CultivationActivity
                    .MaxIssueNotesLength);

        builder.Property(activity =>
                activity.DeviationReason)
            .HasMaxLength(
                CultivationActivity
                    .MaxDeviationReasonLength);

        builder.Property(activity =>
                activity.CancellationReason)
            .HasMaxLength(
                CultivationActivity
                    .MaxCancellationReasonLength);

        builder.Property(activity => activity.Notes)
            .HasMaxLength(
                CultivationActivity.MaxNotesLength);

        builder.Ignore(activity =>
            activity.TotalActualCost);

        builder.Ignore(activity =>
            activity.IsLinkedToSopStep);

        builder.HasIndex(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.CropCycleId,
                    activity.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CultivationActivities_" +
                "OrganizationId_CropCycleId_Code");

        builder.HasIndex(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.CropCycleId,
                    activity.Status
                })
            .HasDatabaseName(
                "IX_CultivationActivities_" +
                "OrganizationId_CropCycleId_Status");

        builder.HasIndex(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.PlannedDate
                })
            .HasDatabaseName(
                "IX_CultivationActivities_" +
                "OrganizationId_PlannedDate");

        builder.HasIndex(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.ActivityType
                })
            .HasDatabaseName(
                "IX_CultivationActivities_" +
                "OrganizationId_ActivityType");

        builder.HasIndex(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.CultivationSopStepId
                })
            .HasDatabaseName(
                "IX_CultivationActivities_" +
                "OrganizationId_CultivationSopStepId");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(activity =>
                activity.OrganizationId)
            .HasConstraintName(
                "FK_CultivationActivities_" +
                "Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.CropCycleId
                })
            .HasPrincipalKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .HasConstraintName(
                "FK_CultivationActivities_" +
                "CropCycles_OrganizationId_CropCycleId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CultivationSop>()
            .WithMany()
            .HasForeignKey(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.CultivationSopId
                })
            .HasPrincipalKey(sop =>
                new
                {
                    sop.OrganizationId,
                    sop.Id
                })
            .HasConstraintName(
                "FK_CultivationActivities_" +
                "CultivationSops_OrganizationId_" +
                "CultivationSopId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CultivationSopStep>()
            .WithMany()
            .HasForeignKey(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.CultivationSopId,
                    activity.CultivationSopStepId
                })
            .HasPrincipalKey(step =>
                new
                {
                    step.OrganizationId,
                    step.CultivationSopId,
                    step.Id
                })
            .HasConstraintName(
                "FK_CultivationActivities_" +
                "CultivationSopSteps_OrganizationId_" +
                "CultivationSopId_CultivationSopStepId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(activity =>
                activity.Resources)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
