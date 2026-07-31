using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Lands;

public sealed class LandConfiguration :
    IEntityTypeConfiguration<Land>
{
    public void Configure(
        EntityTypeBuilder<Land> builder)
    {
        builder.ToTable("Lands");

        builder.ConfigureAuditableEntity();

        builder.Property(land => land.OrganizationId)
            .IsRequired();

        builder.Property(land => land.Code)
            .HasMaxLength(Land.MaxCodeLength)
            .IsRequired();

        builder.Property(land => land.Name)
            .HasMaxLength(Land.MaxNameLength)
            .IsRequired();

        builder.Property(land => land.TenureType)
            .IsRequired();

        builder.Property(land => land.TotalArea)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(land => land.AreaUnit)
            .IsRequired();

        builder.Property(land => land.Address)
            .HasMaxLength(Land.MaxAddressLength);

        builder.Property(land =>
                land.LocationDescription)
            .HasMaxLength(
                Land.MaxLocationDescriptionLength);

        builder.Property(land => land.Latitude)
            .HasPrecision(9, 6);

        builder.Property(land => land.Longitude)
            .HasPrecision(9, 6);

        builder.Property(land => land.Notes)
            .HasMaxLength(Land.MaxNotesLength);

        builder.Property(land => land.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasAlternateKey(land =>
                new
                {
                    land.OrganizationId,
                    land.Id
                })
            .HasName(
                "AK_Lands_OrganizationId_Id");

        builder.HasIndex(land =>
                new
                {
                    land.OrganizationId,
                    land.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_Lands_OrganizationId_Code");

        builder.HasIndex(land =>
                new
                {
                    land.OrganizationId,
                    land.IsActive,
                    land.IsDeleted
                })
            .HasDatabaseName(
                "IX_Lands_OrganizationId_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(land =>
                land.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(land => land.Plots)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
