using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing;

public sealed class ProfitSharingAllocationConfiguration :
    IEntityTypeConfiguration<ProfitSharingAllocation>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingAllocation> builder)
    {
        builder.ToTable("ProfitSharingAllocations");

        builder.HasKey(allocation =>
            allocation.Id);

        builder.Property(allocation =>
                allocation.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.Id
                })
            .HasName(
                "AK_ProfitSharingAllocations_Org_Id");

        builder.Property(allocation =>
                allocation.OrganizationId)
            .IsRequired();

        builder.Property(allocation =>
                allocation.ProfitSharingSettlementId)
            .IsRequired();

        builder.Property(allocation =>
                allocation.ContributorCodeSnapshot)
            .HasMaxLength(
                ProfitSharingAllocation
                    .MaxContributorCodeLength)
            .IsRequired();

        builder.Property(allocation =>
                allocation.ContributorNameSnapshot)
            .HasMaxLength(
                ProfitSharingAllocation
                    .MaxContributorNameLength)
            .IsRequired();

        builder.Property(allocation =>
                allocation.ContributorRole)
            .IsRequired();

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.ConfirmedCapital));

        builder.Property(allocation =>
                allocation.CapitalRatio)
            .HasPrecision(18, 8)
            .IsRequired();

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.CapitalRecovery));

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.CapitalLoss));

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.ManagementProfitShare));

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.CapitalProfitShare));

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.TotalProfitShare));

        ConfigureMoney(
            builder.Property(allocation =>
                allocation.TotalPayout));

        builder.Property(allocation =>
                allocation.Sequence)
            .IsRequired();

        builder.Property(allocation =>
                allocation.CreatedAt)
            .IsRequired();

        builder.HasIndex(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.ProfitSharingSettlementId,
                    allocation.ContributorRole,
                    allocation.ContributorCodeSnapshot
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingAllocations_" +
                "Settlement_Contributor");

        builder.HasIndex(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.ProfitSharingSettlementId,
                    allocation.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingAllocations_" +
                "Settlement_Sequence");

        builder.HasIndex(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.ContributorRole,
                    allocation.ContributorCodeSnapshot
                })
            .HasDatabaseName(
                "IX_ProfitSharingAllocations_" +
                "Org_Contributor");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(allocation =>
                allocation.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingAllocations_Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMoney(
        PropertyBuilder<decimal> propertyBuilder)
    {
        propertyBuilder
            .HasPrecision(18, 2)
            .IsRequired();
    }
}
