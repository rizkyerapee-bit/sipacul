using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Cultivation;

public sealed class CultivationActivityResourceConfiguration :
    IEntityTypeConfiguration<CultivationActivityResource>
{
    public void Configure(
        EntityTypeBuilder<CultivationActivityResource> builder)
    {
        builder.ToTable(
            "CultivationActivityResources");

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id)
            .ValueGeneratedNever();

        builder.Property(resource =>
                resource.OrganizationId)
            .IsRequired();

        builder.Property(resource =>
                resource.CultivationActivityId)
            .IsRequired();

        builder.Property(resource =>
                resource.ResourceType)
            .IsRequired();

        builder.Property(resource =>
                resource.Description)
            .HasMaxLength(
                CultivationActivityResource
                    .MaxDescriptionLength)
            .IsRequired();

        builder.Property(resource =>
                resource.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(resource => resource.Unit)
            .HasMaxLength(
                CultivationActivityResource
                    .MaxUnitLength)
            .IsRequired();

        builder.Property(resource =>
                resource.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(resource =>
                resource.TotalCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(resource => resource.Notes)
            .HasMaxLength(
                CultivationActivityResource
                    .MaxNotesLength);

        builder.Property(resource =>
                resource.CreatedAt)
            .IsRequired();

        builder.Property(resource =>
            resource.UpdatedAt);

        builder.HasIndex(resource =>
                new
                {
                    resource.OrganizationId,
                    resource.CultivationActivityId
                })
            .HasDatabaseName(
                "IX_CultivationActivityResources_" +
                "OrganizationId_CultivationActivityId");

        builder.HasIndex(resource =>
                new
                {
                    resource.OrganizationId,
                    resource.ResourceType
                })
            .HasDatabaseName(
                "IX_CultivationActivityResources_" +
                "OrganizationId_ResourceType");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(resource =>
                resource.OrganizationId)
            .HasConstraintName(
                "FK_CultivationActivityResources_" +
                "Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CultivationActivity>()
            .WithMany(activity =>
                activity.Resources)
            .HasForeignKey(resource =>
                new
                {
                    resource.OrganizationId,
                    resource.CultivationActivityId
                })
            .HasPrincipalKey(activity =>
                new
                {
                    activity.OrganizationId,
                    activity.Id
                })
            .HasConstraintName(
                "FK_CultivationActivityResources_" +
                "CultivationActivities_OrganizationId_" +
                "CultivationActivityId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
