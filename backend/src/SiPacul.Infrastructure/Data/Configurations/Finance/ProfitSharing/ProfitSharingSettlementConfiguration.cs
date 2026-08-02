using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing;

public sealed class ProfitSharingSettlementConfiguration :
    IEntityTypeConfiguration<ProfitSharingSettlement>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingSettlement> builder)
    {
        builder.ToTable("ProfitSharingSettlements");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.Id
                })
            .HasName(
                "AK_ProfitSharingSettlements_Org_Id");

        builder.Property(settlement =>
                settlement.OrganizationId)
            .IsRequired();

        builder.Property(settlement =>
                settlement.CropCycleId)
            .IsRequired();

        builder.Property(settlement =>
                settlement.Code)
            .HasMaxLength(
                ProfitSharingSettlement.MaxCodeLength)
            .IsRequired();

        builder.Property(settlement =>
                settlement.SettlementDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(settlement =>
                settlement.ManagingPartnerCode)
            .HasMaxLength(
                ProfitSharingSettlement
                    .MaxManagingPartnerCodeLength)
            .IsRequired();

        builder.Property(settlement =>
                settlement.ManagingPartnerName)
            .HasMaxLength(
                ProfitSharingSettlement
                    .MaxManagingPartnerNameLength)
            .IsRequired();

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.RecognizedRevenue));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.CollectedRevenue));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.ActivityResourceCost));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.ManualExpenseCost));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalCultivationCost));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.NetProfit));

        builder.Property(settlement =>
                settlement.Outcome)
            .IsRequired();

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.ManagementProfitPool));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.CapitalProfitPool));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalInvestorCapital));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalPartnerCapital));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalCapital));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalCapitalRecovery));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalCapitalLoss));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalInvestorProfitShare));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalPartnerProfitShare));

        ConfigureMoney(
            builder.Property(settlement =>
                settlement.TotalPayout));

        builder.Property(settlement =>
                settlement.CalculationVersion)
            .HasMaxLength(
                ProfitSharingSettlement
                    .MaxCalculationVersionLength)
            .IsRequired();

        builder.Property(settlement =>
                settlement.Notes)
            .HasMaxLength(
                ProfitSharingSettlement.MaxNotesLength);

        builder.Property(settlement =>
                settlement.Status)
            .IsRequired();

        builder.Property(settlement =>
            settlement.FinalizedAt);

        builder.Property(settlement =>
            settlement.VoidedAt);

        builder.Property(settlement =>
                settlement.VoidReason)
            .HasMaxLength(
                ProfitSharingSettlement
                    .MaxVoidReasonLength);

        builder.Ignore(settlement =>
            settlement.OutstandingReceivable);

        builder.Ignore(settlement =>
            settlement.IsActive);

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId,
                    settlement.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSettlements_" +
                "Org_Cycle_Code");

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId,
                    settlement.Status
                })
            .HasDatabaseName(
                "IX_ProfitSharingSettlements_" +
                "Org_Cycle_Status");

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.SettlementDate
                })
            .HasDatabaseName(
                "IX_ProfitSharingSettlements_Org_Date");

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId
                })
            .IsUnique()
            .HasFilter(
                "\"Status\" = 2 AND " +
                "\"IsDeleted\" = false")
            .HasDatabaseName(
                "UX_ProfitSharingSettlements_" +
                "Org_Cycle_Active");

        builder.HasIndex(settlement =>
                settlement.IsDeleted)
            .HasDatabaseName(
                "IX_ProfitSharingSettlements_IsDeleted");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(settlement =>
                settlement.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSettlements_Organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId
                })
            .HasPrincipalKey(cropCycle =>
                new
                {
                    cropCycle.OrganizationId,
                    cropCycle.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingSettlements_CropCycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(settlement =>
                settlement.Allocations)
            .WithOne()
            .HasForeignKey(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.ProfitSharingSettlementId
                })
            .HasPrincipalKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingAllocations_Settlement")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(settlement =>
                settlement.Allocations)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }

    private static void ConfigureMoney(
        PropertyBuilder<decimal> propertyBuilder)
    {
        propertyBuilder
            .HasPrecision(18, 2)
            .IsRequired();
    }
}
