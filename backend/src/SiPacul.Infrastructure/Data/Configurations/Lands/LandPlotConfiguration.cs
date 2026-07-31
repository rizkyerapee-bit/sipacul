using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Lands;

public sealed class LandPlotConfiguration :
    IEntityTypeConfiguration<LandPlot>
{
    public void Configure(
        EntityTypeBuilder<LandPlot> builder)
    {
        builder.ToTable("LandPlots");

        builder.HasKey(plot => plot.Id);

        builder.Property(plot => plot.Id)
            .ValueGeneratedNever();

        builder.Property(plot => plot.OrganizationId)
            .IsRequired();

        builder.Property(plot => plot.LandId)
            .IsRequired();

        builder.Property(plot => plot.Code)
            .HasMaxLength(LandPlot.MaxCodeLength)
            .IsRequired();

        builder.Property(plot => plot.Name)
            .HasMaxLength(LandPlot.MaxNameLength)
            .IsRequired();

        builder.Property(plot => plot.Area)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(plot => plot.AreaUnit)
            .IsRequired();

        builder.Property(plot =>
                plot.GeneralCondition)
            .HasMaxLength(
                LandPlot.MaxGeneralConditionLength);

        builder.Property(plot => plot.Notes)
            .HasMaxLength(LandPlot.MaxNotesLength);

        builder.Property(plot => plot.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(plot => plot.CreatedAt)
            .IsRequired();

        builder.Property(plot => plot.UpdatedAt);

        builder.HasIndex(plot =>
                new
                {
                    plot.OrganizationId,
                    plot.LandId,
                    plot.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_LandPlots_" +
                "OrganizationId_LandId_Code");

        builder.HasIndex(plot =>
                new
                {
                    plot.OrganizationId,
                    plot.LandId,
                    plot.IsActive
                })
            .HasDatabaseName(
                "IX_LandPlots_" +
                "OrganizationId_LandId_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(plot =>
                plot.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Land>()
            .WithMany(land => land.Plots)
            .HasForeignKey(plot =>
                new
                {
                    plot.OrganizationId,
                    plot.LandId
                })
            .HasPrincipalKey(land =>
                new
                {
                    land.OrganizationId,
                    land.Id
                })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
