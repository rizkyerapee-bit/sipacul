using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallResidualShareConfiguration :
    IEntityTypeConfiguration<ProfitSharingWaterfallResidualShareSnapshot>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingWaterfallResidualShareSnapshot> builder)
    {
        builder.ToTable("ProfitSharingWaterfallResidualShares");
        builder.HasKey(share => share.Id);

        builder.Property(share => share.OrganizationId)
            .IsRequired();
        builder.Property(share => share.ProfitSharingWaterfallSettlementId)
            .IsRequired();
        builder.Property(share => share.RecipientCodeSnapshot)
            .HasMaxLength(ProfitSharingWaterfallCalculator.MaxCodeLength)
            .IsRequired();
        builder.Property(share => share.RateNumerator)
            .HasPrecision(18, 8)
            .IsRequired();
        builder.Property(share => share.RateDenominator)
            .HasPrecision(18, 8)
            .IsRequired();
        builder.Property(share => share.Sequence)
            .IsRequired();
        builder.Property(share => share.CreatedAt)
            .IsRequired();

        builder.HasIndex(share =>
                new
                {
                    share.ProfitSharingWaterfallSettlementId,
                    share.RecipientCodeSnapshot
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingWaterfallResidualShares_Settlement_Recipient");
        builder.HasIndex(share =>
                new
                {
                    share.ProfitSharingWaterfallSettlementId,
                    share.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingWaterfallResidualShares_Settlement_Sequence");
    }
}
