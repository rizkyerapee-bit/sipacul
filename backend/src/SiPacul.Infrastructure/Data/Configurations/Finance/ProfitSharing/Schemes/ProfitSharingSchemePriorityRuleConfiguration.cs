using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemePriorityRuleConfiguration :
    IEntityTypeConfiguration<ProfitSharingSchemePriorityRule>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingSchemePriorityRule> builder)
    {
        builder.ToTable("ProfitSharingSchemePriorityRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id)
            .ValueGeneratedNever();

        builder.Property(rule => rule.OrganizationId)
            .IsRequired();

        builder.Property(rule => rule.ProfitSharingSchemeId)
            .IsRequired();

        builder.Property(rule => rule.RuleCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        builder.Property(rule => rule.RuleType)
            .IsRequired();

        builder.Property(rule => rule.RecipientCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        ConfigureRate(builder.Property(rule => rule.RateNumerator));
        ConfigureRate(builder.Property(rule => rule.RateDenominator));

        builder.Ignore(rule => rule.Rate);

        builder.Property(rule => rule.Sequence)
            .IsRequired();

        builder.Property(rule => rule.CreatedAt)
            .IsRequired();

        builder.HasIndex(rule =>
                new
                {
                    rule.OrganizationId,
                    rule.ProfitSharingSchemeId,
                    rule.RuleCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemePriorityRules_Scheme_Code");

        builder.HasIndex(rule =>
                new
                {
                    rule.OrganizationId,
                    rule.ProfitSharingSchemeId,
                    rule.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemePriorityRules_Scheme_Sequence");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(rule => rule.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemePriorityRules_Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRate(
        PropertyBuilder<decimal> propertyBuilder)
    {
        propertyBuilder.HasPrecision(18, 8).IsRequired();
    }
}
