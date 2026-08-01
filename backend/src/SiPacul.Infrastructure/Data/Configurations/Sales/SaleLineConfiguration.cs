using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Configurations.Sales;

public sealed class SaleLineConfiguration :
    IEntityTypeConfiguration<SaleLine>
{
    public void Configure(
        EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id)
            .ValueGeneratedNever();

        builder.Property(line => line.OrganizationId)
            .IsRequired();

        builder.Property(line => line.SaleId)
            .IsRequired();

        builder.Property(line => line.HarvestBatchId)
            .IsRequired();

        builder.Property(line =>
                line.HarvestBatchCodeSnapshot)
            .HasMaxLength(
                SaleLine.MaxHarvestBatchCodeLength)
            .IsRequired();

        builder.Property(line =>
                line.CropCycleIdSnapshot)
            .IsRequired();

        builder.Property(line =>
                line.CropCycleCodeSnapshot)
            .HasMaxLength(
                SaleLine.MaxCropCycleCodeLength)
            .IsRequired();

        builder.Property(line =>
                line.CommodityIdSnapshot)
            .IsRequired();

        builder.Property(line =>
                line.CommodityCodeSnapshot)
            .HasMaxLength(
                SaleLine.MaxCommodityCodeLength)
            .IsRequired();

        builder.Property(line =>
                line.CommodityNameSnapshot)
            .HasMaxLength(
                SaleLine.MaxCommodityNameLength)
            .IsRequired();

        builder.Property(line =>
                line.QualityGradeSnapshot)
            .HasMaxLength(
                SaleLine.MaxQualityGradeLength);

        builder.Property(line => line.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(line => line.QuantityUnit)
            .IsRequired();

        builder.Property(line => line.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(line => line.LineDiscount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(line => line.LineTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(line => line.Notes)
            .HasMaxLength(SaleLine.MaxNotesLength);

        builder.Property(line => line.CreatedAt)
            .IsRequired();

        builder.Property(line => line.UpdatedAt);

        builder.HasIndex(line =>
                new
                {
                    line.OrganizationId,
                    line.SaleId,
                    line.HarvestBatchId
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_SaleLines_OrganizationId_" +
                "SaleId_HarvestBatchId");

        builder.HasIndex(line =>
                new
                {
                    line.OrganizationId,
                    line.SaleId
                })
            .HasDatabaseName(
                "IX_SaleLines_OrganizationId_SaleId");

        builder.HasIndex(line =>
                new
                {
                    line.OrganizationId,
                    line.HarvestBatchId
                })
            .HasDatabaseName(
                "IX_SaleLines_OrganizationId_" +
                "HarvestBatchId");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(line =>
                line.OrganizationId)
            .HasConstraintName(
                "FK_SaleLines_Organizations_" +
                "OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<HarvestBatch>()
            .WithMany()
            .HasForeignKey(line =>
                new
                {
                    line.OrganizationId,
                    line.HarvestBatchId
                })
            .HasPrincipalKey(batch =>
                new
                {
                    batch.OrganizationId,
                    batch.Id
                })
            .HasConstraintName(
                "FK_SaleLines_HarvestBatches_" +
                "OrganizationId_HarvestBatchId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
