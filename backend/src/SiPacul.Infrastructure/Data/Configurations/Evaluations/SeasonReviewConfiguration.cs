using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data.Configurations.Common;

namespace SiPacul.Infrastructure.Data.Configurations.Evaluations;

public sealed class SeasonReviewConfiguration :
    IEntityTypeConfiguration<SeasonReview>
{
    public void Configure(
        EntityTypeBuilder<SeasonReview> builder)
    {
        builder.ToTable("SeasonReviews");

        builder.ConfigureAuditableEntity();

        builder.HasAlternateKey(review =>
                new
                {
                    review.OrganizationId,
                    review.Id
                })
            .HasName(
                "AK_SeasonReviews_OrganizationId_Id");

        builder.Property(review => review.OrganizationId)
            .IsRequired();

        builder.Property(review => review.CropCycleId)
            .IsRequired();

        builder.Property(review => review.ReviewDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(review => review.Findings)
            .HasMaxLength(SeasonReview.MaxFindingsLength)
            .IsRequired();

        builder.Property(review => review.LessonsLearned)
            .HasMaxLength(
                SeasonReview.MaxLessonsLearnedLength)
            .IsRequired();

        builder.Property(review =>
                review.NextSeasonRecommendations)
            .HasMaxLength(
                SeasonReview
                    .MaxNextSeasonRecommendationsLength)
            .IsRequired();

        builder.Property(review => review.Status)
            .IsRequired()
            .HasDefaultValue(SeasonReviewStatus.Draft);

        builder.Property(review => review.FinalizedAt);

        builder.Ignore(review => review.IsFinalized);

        builder.HasIndex(review =>
                new
                {
                    review.OrganizationId,
                    review.CropCycleId
                },
                "UX_SeasonReviews_" +
                "OrganizationId_CropCycleId_Active")
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName(
                "UX_SeasonReviews_" +
                "OrganizationId_CropCycleId_Active");

        builder.HasIndex(review =>
                new
                {
                    review.OrganizationId,
                    review.Status,
                    review.ReviewDate
                })
            .HasDatabaseName(
                "IX_SeasonReviews_" +
                "OrganizationId_Status_ReviewDate");

        builder.HasIndex(review => review.IsDeleted)
            .HasDatabaseName(
                "IX_SeasonReviews_IsDeleted");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(review => review.OrganizationId)
            .HasConstraintName(
                "FK_SeasonReviews_" +
                "Organizations_OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CropCycle>()
            .WithMany()
            .HasForeignKey(review =>
                new
                {
                    review.OrganizationId,
                    review.CropCycleId
                })
            .HasPrincipalKey(cycle =>
                new
                {
                    cycle.OrganizationId,
                    cycle.Id
                })
            .HasConstraintName(
                "FK_SeasonReviews_CropCycles_" +
                "OrganizationId_CropCycleId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
