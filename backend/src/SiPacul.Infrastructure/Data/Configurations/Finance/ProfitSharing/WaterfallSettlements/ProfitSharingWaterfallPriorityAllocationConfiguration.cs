using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallPriorityAllocationConfiguration :
    IEntityTypeConfiguration<ProfitSharingWaterfallPriorityAllocation>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingWaterfallPriorityAllocation> builder)
    {
        builder.ToTable("ProfitSharingWaterfallPriorityAllocations");
        builder.HasKey(allocation => allocation.Id);

        builder.Property(allocation => allocation.OrganizationId)
            .IsRequired();
        builder.Property(allocation =>
                allocation.ProfitSharingWaterfallSettlementId)
            .IsRequired();
        builder.Property(allocation => allocation.RuleCode)
            .HasMaxLength(ProfitSharingWaterfallCalculator.MaxCodeLength)
            .IsRequired();
        builder.Property(allocation => allocation.RuleType)
            .IsRequired();
        builder.Property(allocation => allocation.RecipientCodeSnapshot)
            .HasMaxLength(ProfitSharingWaterfallCalculator.MaxCodeLength)
            .IsRequired();
        builder.Property(allocation => allocation.RecipientNameSnapshot)
            .HasMaxLength(ProfitSharingWaterfallCalculator.MaxNameLength)
            .IsRequired();
        builder.Property(allocation => allocation.RateNumerator)
            .HasPrecision(18, 8)
            .IsRequired();
        builder.Property(allocation => allocation.RateDenominator)
            .HasPrecision(18, 8)
            .IsRequired();

        foreach (var propertyName in MoneyPropertyNames)
        {
            builder.Property<decimal>(propertyName)
                .HasPrecision(18, 2)
                .IsRequired();
        }

        builder.Property(allocation => allocation.Sequence)
            .IsRequired();
        builder.Property(allocation => allocation.CreatedAt)
            .IsRequired();

        builder.HasIndex(allocation =>
                new
                {
                    allocation.ProfitSharingWaterfallSettlementId,
                    allocation.RuleCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSWaterfallPriorityAlloc_Settlement_Rule");
        builder.HasIndex(allocation =>
                new
                {
                    allocation.ProfitSharingWaterfallSettlementId,
                    allocation.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSWaterfallPriorityAlloc_Settlement_Sequence");
    }

    private static readonly string[] MoneyPropertyNames =
    [
        nameof(ProfitSharingWaterfallPriorityAllocation.BaseAmount),
        nameof(ProfitSharingWaterfallPriorityAllocation.RequestedAmount),
        nameof(ProfitSharingWaterfallPriorityAllocation.AllocatedAmount),
        nameof(ProfitSharingWaterfallPriorityAllocation.UnallocatedAmount)
    ];
}
