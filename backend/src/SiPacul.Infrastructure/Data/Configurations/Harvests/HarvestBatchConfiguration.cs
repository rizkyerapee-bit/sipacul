using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Harvests;

public sealed class HarvestBatchConfiguration :
    IEntityTypeConfiguration<HarvestBatch>
{
    public void Configure(
        EntityTypeBuilder<HarvestBatch> builder)
    {
        builder.ToTable("HarvestBatches");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.Id
                })
            .HasName(
                "AK_HarvestBatches_OrganizationId_Id");

        builder.Property(batch => batch.OrganizationId)
            .IsRequired();

        builder.Property(batch => batch.CropCycleId)
            .IsRequired();

        builder.Property(batch => batch.Code)
            .HasMaxLength(HarvestBatch.MaxCodeLength)
            .IsRequired();

        builder.Property(batch => batch.HarvestDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(batch => batch.GrossQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(batch => batch.RejectedQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(batch => batch.NetQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(batch => batch.QuantityUnit)
            .IsRequired();

        builder.Property(batch => batch.QualityGrade)
            .HasMaxLength(
                HarvestBatch.MaxQualityGradeLength);

        builder.Property(batch => batch.StorageLocation)
            .HasMaxLength(
                HarvestBatch.MaxStorageLocationLength);

        builder.Property(batch => batch.Notes)
            .HasMaxLength(HarvestBatch.MaxNotesLength);

        builder.Property(batch => batch.Status)
            .IsRequired()
            .HasDefaultValue(
                HarvestBatchStatus.Draft);

        builder.Property(batch => batch.ConfirmedAt);

        builder.Property(batch =>
                batch.CancellationReason)
            .HasMaxLength(
                HarvestBatch.MaxCancellationReasonLength);

        builder.Ignore(batch => batch.IsSellable);

        builder.HasIndex(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.CropCycleId,
                    batch.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_HarvestBatches_" +
                "OrganizationId_CropCycleId_Code");

        builder.HasIndex(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.CropCycleId,
                    batch.Status
                })
            .HasDatabaseName(
                "IX_HarvestBatches_" +
                "OrganizationId_CropCycleId_Status");

        builder.HasIndex(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.HarvestDate
                })
            .HasDatabaseName(
                "IX_HarvestBatches_" +
                "OrganizationId_HarvestDate");

        builder.HasIndex(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.Status
                })
            .HasDatabaseName(
                "IX_HarvestBatches_" +
                "OrganizationId_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(batch =>
                batch.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.CropCycleId
                })
            .HasPrincipalKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
