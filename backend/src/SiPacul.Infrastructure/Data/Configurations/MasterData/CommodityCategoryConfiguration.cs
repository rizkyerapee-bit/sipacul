using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.MasterData;

public sealed class CommodityCategoryConfiguration :
    IEntityTypeConfiguration<CommodityCategory>
{
    public void Configure(
        EntityTypeBuilder<CommodityCategory> builder)
    {
        builder.ToTable("CommodityCategories");

        builder.ConfigureAuditableEntity();

        builder.Property(category => category.OrganizationId)
            .IsRequired();

        builder.Property(category => category.Name)
            .HasMaxLength(CommodityCategory.MaxNameLength)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(
                CommodityCategory.MaxDescriptionLength);

        builder.Property(category => category.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasAlternateKey(category =>
                new
                {
                    category.OrganizationId,
                    category.Id
                })
            .HasName(
                "AK_CommodityCategories_OrganizationId_Id");

        builder.HasIndex(category =>
                new
                {
                    category.OrganizationId,
                    category.Name
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CommodityCategories_OrganizationId_Name");

        builder.HasIndex(category =>
                new
                {
                    category.OrganizationId,
                    category.IsActive,
                    category.IsDeleted
                })
            .HasDatabaseName(
                "IX_CommodityCategories_OrganizationId_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(category =>
                category.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
