using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Assignments;

public sealed class
    ProfitSharingSchemeAssignmentPriorityRuleConfiguration :
    IEntityTypeConfiguration<
        ProfitSharingSchemeAssignmentPriorityRule>
{
    public void Configure(
        EntityTypeBuilder<
            ProfitSharingSchemeAssignmentPriorityRule> builder)
    {
        builder.ToTable(
            "ProfitSharingSchemeAssignmentPriorityRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();

        builder.Property(rule => rule.OrganizationId)
            .IsRequired();

        builder.Property(rule =>
                rule.ProfitSharingSchemeAssignmentId)
            .IsRequired();

        builder.Property(rule => rule.RuleCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(rule => rule.RuleType)
            .IsRequired();

        builder.Property(rule => rule.RecipientCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(rule => rule.RateNumerator)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(rule => rule.RateDenominator)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(rule => rule.Sequence)
            .IsRequired();

        builder.Property(rule => rule.CreatedAt)
            .IsRequired();

        builder.HasIndex(rule =>
                new
                {
                    rule.OrganizationId,
                    rule.ProfitSharingSchemeAssignmentId,
                    rule.RuleCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSAssignmentPriorityRules_Assignment_Code");

        builder.HasIndex(rule =>
                new
                {
                    rule.OrganizationId,
                    rule.ProfitSharingSchemeAssignmentId,
                    rule.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PSAssignmentPriorityRules_Assignment_Sequence");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(rule => rule.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemeAssignmentPriorityRules_" +
                "Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
