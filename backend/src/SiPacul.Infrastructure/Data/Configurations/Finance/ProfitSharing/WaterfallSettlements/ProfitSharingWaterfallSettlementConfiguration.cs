using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallSettlementConfiguration :
    IEntityTypeConfiguration<ProfitSharingWaterfallSettlement>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingWaterfallSettlement> builder)
    {
        builder.ToTable("ProfitSharingWaterfallSettlements");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.Id
                })
            .HasName(
                "AK_ProfitSharingWaterfallSettlements_Org_Id");

        builder.Property(settlement => settlement.OrganizationId)
            .IsRequired();
        builder.Property(settlement => settlement.CropCycleId)
            .IsRequired();
        builder.Property(settlement => settlement.AssignmentId)
            .IsRequired();
        builder.Property(settlement => settlement.SourceSchemeId)
            .IsRequired();
        builder.Property(settlement => settlement.SchemeFamilyId)
            .IsRequired();

        builder.Property(settlement => settlement.Code)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement.MaxCodeLength)
            .IsRequired();

        builder.Property(settlement => settlement.SettlementDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(settlement => settlement.SchemeCodeSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement.MaxSchemeCodeLength)
            .IsRequired();

        builder.Property(settlement => settlement.SchemeNameSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement.MaxSchemeNameLength)
            .IsRequired();

        builder.Property(settlement =>
                settlement.SchemeDescriptionSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement
                    .MaxSchemeDescriptionLength);

        builder.Property(settlement => settlement.SchemeVersionSnapshot)
            .IsRequired();
        builder.Property(settlement => settlement.SchemeAssignedAtSnapshot)
            .IsRequired();
        builder.Property(settlement => settlement.ResidualMethod)
            .IsRequired();

        builder.Property(settlement =>
                settlement.ResidualRecipientCodeSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement.MaxSchemeCodeLength);

        builder.Property(settlement => settlement.CropCycleCodeSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallCalculator.MaxCodeLength)
            .IsRequired();
        builder.Property(settlement => settlement.CropCycleNameSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallCalculator.MaxNameLength)
            .IsRequired();
        builder.Property(settlement => settlement.CommodityIdSnapshot)
            .IsRequired();
        builder.Property(settlement => settlement.CommodityCodeSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallCalculator.MaxCodeLength)
            .IsRequired();
        builder.Property(settlement => settlement.CommodityNameSnapshot)
            .HasMaxLength(
                ProfitSharingWaterfallCalculator.MaxNameLength)
            .IsRequired();

        foreach (var propertyName in MoneyPropertyNames)
        {
            builder.Property<decimal>(propertyName)
                .HasPrecision(18, 2)
                .IsRequired();
        }

        builder.Property(settlement => settlement.Outcome)
            .IsRequired();

        builder.Property(settlement => settlement.AvailableHarvestQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(settlement => settlement.CalculationVersion)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement
                    .MaxCalculationVersionLength)
            .IsRequired();
        builder.Property(settlement => settlement.CalculatedAt)
            .IsRequired();
        builder.Property(settlement => settlement.Notes)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement.MaxNotesLength);
        builder.Property(settlement => settlement.Status)
            .IsRequired();
        builder.Property(settlement => settlement.FinalizedAt)
            .IsRequired();
        builder.Property(settlement => settlement.VoidedAt);
        builder.Property(settlement => settlement.VoidReason)
            .HasMaxLength(
                ProfitSharingWaterfallSettlement.MaxVoidReasonLength);

        builder.Ignore(settlement => settlement.IsActive);

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId,
                    settlement.Code
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Code");

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId,
                    settlement.Status
                })
            .HasDatabaseName(
                "IX_ProfitSharingWaterfallSettlements_Org_Cycle_Status");

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.SettlementDate
                })
            .HasDatabaseName(
                "IX_ProfitSharingWaterfallSettlements_Org_Date");

        builder.HasIndex(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId
                })
            .IsUnique()
            .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = false")
            .HasDatabaseName(
                "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Active");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(settlement => settlement.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingWaterfallSettlements_Organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.CropCycleId
                })
            .HasPrincipalKey(cycle =>
                new
                {
                    cycle.OrganizationId,
                    cycle.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingWaterfallSettlements_CropCycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProfitSharingSchemeAssignment>()
            .WithMany()
            .HasForeignKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.AssignmentId
                })
            .HasPrincipalKey(assignment =>
                new
                {
                    assignment.OrganizationId,
                    assignment.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingWaterfallSettlements_Assignment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProfitSharingScheme>()
            .WithMany()
            .HasForeignKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.SourceSchemeId
                })
            .HasPrincipalKey(scheme =>
                new
                {
                    scheme.OrganizationId,
                    scheme.Id
                })
            .HasConstraintName(
                "FK_ProfitSharingWaterfallSettlements_SourceScheme")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(settlement => settlement.PriorityAllocations)
            .WithOne()
            .HasForeignKey(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.ProfitSharingWaterfallSettlementId
                })
            .HasPrincipalKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.Id
                })
            .HasConstraintName(
                "FK_PSWaterfallPriorityAlloc_Settlement")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(settlement => settlement.ParticipantAllocations)
            .WithOne()
            .HasForeignKey(allocation =>
                new
                {
                    allocation.OrganizationId,
                    allocation.ProfitSharingWaterfallSettlementId
                })
            .HasPrincipalKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.Id
                })
            .HasConstraintName(
                "FK_PSWaterfallParticipantAlloc_Settlement")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(settlement => settlement.ResidualShares)
            .WithOne()
            .HasForeignKey(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingWaterfallSettlementId
                })
            .HasPrincipalKey(settlement =>
                new
                {
                    settlement.OrganizationId,
                    settlement.Id
                })
            .HasConstraintName(
                "FK_PSWaterfallResidualShare_Settlement")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(settlement => settlement.PriorityAllocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(settlement => settlement.ParticipantAllocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(settlement => settlement.ResidualShares)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static readonly string[] MoneyPropertyNames =
    [
        nameof(ProfitSharingWaterfallSettlement.RecognizedRevenue),
        nameof(ProfitSharingWaterfallSettlement.CollectedRevenue),
        nameof(ProfitSharingWaterfallSettlement.OutstandingReceivable),
        nameof(ProfitSharingWaterfallSettlement.ActivityResourceCost),
        nameof(ProfitSharingWaterfallSettlement.ManualExpenseCost),
        nameof(ProfitSharingWaterfallSettlement.TotalCultivationCost),
        nameof(ProfitSharingWaterfallSettlement.NetProfit),
        nameof(ProfitSharingWaterfallSettlement.ConfirmedInvestorCapital),
        nameof(ProfitSharingWaterfallSettlement.ConfirmedPartnerCapital),
        nameof(ProfitSharingWaterfallSettlement.TotalConfirmedCapital),
        nameof(ProfitSharingWaterfallSettlement.TotalCapital),
        nameof(ProfitSharingWaterfallSettlement.TotalCapitalRecovery),
        nameof(ProfitSharingWaterfallSettlement.TotalCapitalLoss),
        nameof(ProfitSharingWaterfallSettlement.TotalManagementProfitShare),
        nameof(ProfitSharingWaterfallSettlement.TotalReturnOnCapitalProfitShare),
        nameof(ProfitSharingWaterfallSettlement.TotalPriorityProfitShare),
        nameof(ProfitSharingWaterfallSettlement.TotalResidualProfitShare),
        nameof(ProfitSharingWaterfallSettlement.TotalProfitShare),
        nameof(ProfitSharingWaterfallSettlement.TotalPayout)
    ];
}
