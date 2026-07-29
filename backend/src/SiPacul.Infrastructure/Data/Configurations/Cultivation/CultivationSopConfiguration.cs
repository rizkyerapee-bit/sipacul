using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Cultivation;

public sealed class CultivationSopConfiguration :
    IEntityTypeConfiguration<CultivationSop>
{
    public void Configure(
        EntityTypeBuilder<CultivationSop> builder)
    {
        builder.ToTable("CultivationSops");

        builder.ConfigureAuditableEntity();

        builder.Property(sop => sop.OrganizationId)
            .IsRequired();

        builder.Property(sop => sop.CommodityId)
            .IsRequired();

        builder.Property(sop => sop.Name)
            .HasMaxLength(CultivationSop.MaxNameLength)
            .IsRequired();

        builder.Property(sop => sop.Description)
            .HasMaxLength(
                CultivationSop.MaxDescriptionLength);

        builder.Property(sop => sop.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(sop =>
                new
                {
                    sop.OrganizationId,
                    sop.CommodityId,
                    sop.Name
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_CultivationSops_" +
                "OrganizationId_CommodityId_Name");

        builder.HasIndex(sop =>
                new
                {
                    sop.OrganizationId,
                    sop.CommodityId,
                    sop.IsActive,
                    sop.IsDeleted
                })
            .HasDatabaseName(
                "IX_CultivationSops_" +
                "OrganizationId_CommodityId_Status");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(sop =>
                sop.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Commodity>()
            .WithMany()
            .HasForeignKey(sop =>
                new
                {
                    sop.OrganizationId,
                    sop.CommodityId
                })
            .HasPrincipalKey(commodity =>
                new
                {
                    commodity.OrganizationId,
                    commodity.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(sop => sop.Steps)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
