using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Cultivation;

public sealed class CropCycleConfiguration :
    IEntityTypeConfiguration<CropCycle>
{
    public void Configure(
        EntityTypeBuilder<CropCycle> builder)
    {
        builder.ToTable("CropCycles");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .HasName(
                "AK_CropCycles_OrganizationId_Id");

        builder.Property(cropCycle =>
                cropCycle.OrganizationId)
            .IsRequired();

        builder.Property(cropCycle => cropCycle.Code)
            .HasMaxLength(CropCycle.MaxCodeLength)
            .IsRequired();

        builder.Property(cropCycle => cropCycle.Name)
            .HasMaxLength(CropCycle.MaxNameLength)
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.CommodityId)
            .IsRequired();

        builder.Property(cropCycle =>
            cropCycle.CultivationSopId);

        builder.Property(cropCycle => cropCycle.LandId)
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.LandPlotId)
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.PlantedArea)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.AreaUnit)
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.PlannedStartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.ExpectedHarvestDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(cropCycle =>
                cropCycle.ActualStartDate)
            .HasColumnType("date");

        builder.Property(cropCycle =>
                cropCycle.ActualHarvestDate)
            .HasColumnType("date");

        builder.Property(cropCycle => cropCycle.Status)
            .IsRequired()
            .HasDefaultValue(
                CropCycleStatus.Planned);

        builder.Property(cropCycle =>
                cropCycle.CancellationReason)
            .HasMaxLength(
                CropCycle.MaxCancellationReasonLength);

        builder.Property(cropCycle => cropCycle.Notes)
            .HasMaxLength(CropCycle.MaxNotesLength);

        builder.HasIndex(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CropCycles_OrganizationId_Code");

        builder.HasIndex(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Status
                })
            .HasDatabaseName(
                "IX_CropCycles_OrganizationId_Status");

        builder.HasIndex(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.CommodityId
                })
            .HasDatabaseName(
                "IX_CropCycles_" +
                "OrganizationId_CommodityId");

        builder.HasIndex(
                cropCycle =>
                    new
                    {
                        cropCycle.OrganizationId,
                        cropCycle.LandId,
                        cropCycle.LandPlotId
                    },
                "IX_CropCycles_" +
                "OrganizationId_LandId_LandPlotId")
            .HasDatabaseName(
                "IX_CropCycles_" +
                "OrganizationId_LandId_LandPlotId");

        builder.HasIndex(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.PlannedStartDate,
                    cropCycle.ExpectedHarvestDate
                })
            .HasDatabaseName(
                "IX_CropCycles_" +
                "OrganizationId_PlannedDateRange");

        builder.HasIndex(
                cropCycle =>
                    new
                    {
                        cropCycle.OrganizationId,
                        cropCycle.LandId,
                        cropCycle.LandPlotId
                    },
                "UX_CropCycles_" +
                "OrganizationId_LandId_" +
                "LandPlotId_InProgress")
            .IsUnique()
            .HasFilter(
                "\"Status\" = 2 AND " +
                "\"IsDeleted\" = FALSE")
            .HasDatabaseName(
                "UX_CropCycles_" +
                "OrganizationId_LandId_" +
                "LandPlotId_InProgress");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(cropCycle =>
                cropCycle.OrganizationId)
            .HasConstraintName(
                "FK_CropCycles_" +
                "Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Commodity>()
            .WithMany()
            .HasForeignKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.CommodityId
                })
            .HasPrincipalKey(commodity =>
                new
                {
                    commodity.OrganizationId,
                    commodity.Id
                })
            .HasConstraintName(
                "FK_CropCycles_Commodities_" +
                "OrganizationId_CommodityId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CultivationSop>()
            .WithMany()
            .HasForeignKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.CultivationSopId
                })
            .HasPrincipalKey(sop =>
                new
                {
                    sop.OrganizationId,
                    sop.Id
                })
            .HasConstraintName(
                "FK_CropCycles_CultivationSops_" +
                "OrganizationId_CultivationSopId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Land>()
            .WithMany()
            .HasForeignKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.LandId
                })
            .HasPrincipalKey(land =>
                new
                {
                    land.OrganizationId,
                    land.Id
                })
            .HasConstraintName(
                "FK_CropCycles_Lands_" +
                "OrganizationId_LandId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LandPlot>()
            .WithMany()
            .HasForeignKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.LandId,
                    cropCycle.LandPlotId
                })
            .HasPrincipalKey(plot =>
                new
                {
                    plot.OrganizationId,
                    plot.LandId,
                    plot.Id
                })
            .HasConstraintName(
                "FK_CropCycles_LandPlots_" +
                "OrganizationId_LandId_LandPlotId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
