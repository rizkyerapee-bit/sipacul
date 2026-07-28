using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Organizations;

public sealed class OrganizationConfiguration :
    IEntityTypeConfiguration<Organization>
{
    public void Configure(
        EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.ConfigureAuditableEntity();

        builder.Property(organization => organization.Code)
            .HasMaxLength(Organization.MaxCodeLength)
            .IsRequired();

        builder.Property(organization => organization.Name)
            .HasMaxLength(Organization.MaxNameLength)
            .IsRequired();

        builder.Property(organization => organization.LegalName)
            .HasMaxLength(Organization.MaxLegalNameLength);

        builder.Property(organization => organization.TimeZone)
            .HasMaxLength(Organization.MaxTimeZoneLength)
            .IsRequired();

        builder.Property(organization => organization.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(organization => organization.Code)
            .IsUnique()
            .HasDatabaseName("UX_Organizations_Code");

        builder.HasIndex(organization =>
                new
                {
                    organization.IsActive,
                    organization.IsDeleted
                })
            .HasDatabaseName(
                "IX_Organizations_Status");
    }
}
