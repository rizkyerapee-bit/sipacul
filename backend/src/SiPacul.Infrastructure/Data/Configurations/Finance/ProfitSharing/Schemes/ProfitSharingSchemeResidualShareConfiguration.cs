using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Configurations.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemeResidualShareConfiguration :
    IEntityTypeConfiguration<ProfitSharingSchemeResidualShare>
{
    public void Configure(
        EntityTypeBuilder<ProfitSharingSchemeResidualShare> builder)
    {
        builder.ToTable("ProfitSharingSchemeResidualShares");

        builder.HasKey(share => share.Id);
        builder.Property(share => share.Id)
            .ValueGeneratedNever();

        builder.Property(share => share.OrganizationId)
            .IsRequired();

        builder.Property(share => share.ProfitSharingSchemeId)
            .IsRequired();

        builder.Property(share => share.RecipientCode)
            .HasMaxLength(ProfitSharingScheme.MaxCodeLength)
            .IsRequired();

        ConfigureRate(builder.Property(share => share.RateNumerator));
        ConfigureRate(builder.Property(share => share.RateDenominator));

        builder.Ignore(share => share.Rate);

        builder.Property(share => share.Sequence)
            .IsRequired();

        builder.Property(share => share.CreatedAt)
            .IsRequired();

        builder.HasIndex(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingSchemeId,
                    share.RecipientCode
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemeResidualShares_Scheme_Recipient");

        builder.HasIndex(share =>
                new
                {
                    share.OrganizationId,
                    share.ProfitSharingSchemeId,
                    share.Sequence
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_ProfitSharingSchemeResidualShares_Scheme_Sequence");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(share => share.OrganizationId)
            .HasConstraintName(
                "FK_ProfitSharingSchemeResidualShares_Organization")
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRate(
        PropertyBuilder<decimal> propertyBuilder)
    {
        propertyBuilder.HasPrecision(18, 8).IsRequired();
    }
}
