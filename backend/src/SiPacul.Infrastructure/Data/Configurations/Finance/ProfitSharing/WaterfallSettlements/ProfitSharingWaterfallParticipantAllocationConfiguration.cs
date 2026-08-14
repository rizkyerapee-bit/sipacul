using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallParticipantAllocationConfiguration :
    IEntityTypeConfiguration<ProfitSharingWaterfallParticipantAllocation>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingWaterfallParticipantAllocation> builder)
    {
        builder.ToTable("ProfitSharingWaterfallParticipantAllocations");
        builder.HasKey(allocation => allocation.Id);

        builder.Property(allocation => allocation.OrganizationId)
            .IsRequired();
        builder.Property(allocation =>
                allocation.ProfitSharingWaterfallSettlementId)
            .IsRequired();
        builder.Property(allocation => allocation.ParticipantCodeSnapshot)
            .HasMaxLength(ProfitSharingWaterfallCalculator.MaxCodeLength)
            .IsRequired();
        builder.Property(allocation => allocation.ParticipantNameSnapshot)
            .HasMaxLength(ProfitSharingWaterfallCalculator.MaxNameLength)
            .IsRequired();
        builder.Property(allocation => allocation.ParticipantRole)
            .IsRequired();
        builder.Property(allocation => allocation.ConfirmedCapital)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(allocation => allocation.CapitalRatio)
            .HasPrecision(18, 8)
            .IsRequired();
        builder.Property(allocation =>
                allocation.ParticipatesInResidualProfit)
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
                    allocation.ParticipantCodeSnapshot
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSWaterfallParticipantAlloc_Settlement_Participant");
        builder.HasIndex(allocation =>
                new
                {
                    allocation.ProfitSharingWaterfallSettlementId,
                    allocation.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSWaterfallParticipantAlloc_Settlement_Sequence");
    }

    private static readonly string[] MoneyPropertyNames =
    [
        nameof(ProfitSharingWaterfallParticipantAllocation.CapitalRecovery),
        nameof(ProfitSharingWaterfallParticipantAllocation.CapitalLoss),
        nameof(ProfitSharingWaterfallParticipantAllocation.ManagementProfitShare),
        nameof(ProfitSharingWaterfallParticipantAllocation.ReturnOnCapitalProfitShare),
        nameof(ProfitSharingWaterfallParticipantAllocation.ResidualProfitShare),
        nameof(ProfitSharingWaterfallParticipantAllocation.TotalProfitShare),
        nameof(ProfitSharingWaterfallParticipantAllocation.TotalPayout)
    ];
}
