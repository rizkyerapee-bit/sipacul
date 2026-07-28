using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Domain.Common.Base;

namespace SiPacul.Infrastructure.Data.Configurations.Common;

internal static class AuditableEntityConfigurationExtensions
{
    private const int UserIdentifierMaxLength = 150;

    public static void ConfigureAuditableEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(UserIdentifierMaxLength);

        builder.Property(entity => entity.UpdatedAt);

        builder.Property(entity => entity.UpdatedBy)
            .HasMaxLength(UserIdentifierMaxLength);

        builder.Property(entity => entity.DeletedAt);

        builder.Property(entity => entity.DeletedBy)
            .HasMaxLength(UserIdentifierMaxLength);

        builder.Property(entity => entity.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(entity => entity.IsDeleted);
    }
}
