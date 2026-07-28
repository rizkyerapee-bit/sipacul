using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.MasterData;

public sealed class CommodityConfiguration :
    IEntityTypeConfiguration<Commodity>
{
    public void Configure(
        EntityTypeBuilder<Commodity> builder)
    {
        builder.ToTable("Commodities");

        builder.ConfigureAuditableEntity();

        builder.Property(commodity => commodity.OrganizationId)
            .IsRequired();

        builder.Property(commodity => commodity.Code)
            .HasConversion(
                code => code.Value,
                value => CommodityCode.Create(value))
            .HasMaxLength(CommodityCode.MaxLength)
            .IsRequired();

        builder.Property(commodity => commodity.Name)
            .IsRequired();

        builder.Property(commodity =>
            commodity.ScientificName);

        builder.Property(commodity =>
            commodity.Description);

        builder.Property(commodity =>
                commodity.CommodityCategoryId)
            .IsRequired();

        builder.Property(commodity => commodity.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(commodity =>
                new
                {
                    commodity.OrganizationId,
                    commodity.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_Commodities_OrganizationId_Code");

        builder.HasIndex(commodity =>
                new
                {
                    commodity.OrganizationId,
                    commodity.IsActive,
                    commodity.IsDeleted
                })
            .HasDatabaseName(
                "IX_Commodities_OrganizationId_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(commodity =>
                commodity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(commodity =>
                commodity.CommodityCategory)
            .WithMany(category =>
                category.Commodities)
            .HasForeignKey(commodity =>
                new
                {
                    commodity.OrganizationId,
                    commodity.CommodityCategoryId
                })
            .HasPrincipalKey(category =>
                new
                {
                    category.OrganizationId,
                    category.Id
                })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
