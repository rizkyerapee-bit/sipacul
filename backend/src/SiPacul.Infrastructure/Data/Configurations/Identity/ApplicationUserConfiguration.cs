using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Infrastructure.Data.Configurations.Identity;

public sealed class ApplicationUserConfiguration :
    IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(
        EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id)
            .HasName("PK_Users");

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.UserName)
            .HasMaxLength(
                ApplicationUser.MaxEmailLength);

        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(
                ApplicationUser.MaxEmailLength);

        builder.Property(user => user.Email)
            .HasMaxLength(
                ApplicationUser.MaxEmailLength);

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(
                ApplicationUser.MaxEmailLength);

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.LastLoginAt);

        builder.Property(user => user.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName(
                "UX_Users_NormalizedUserName");

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName(
                "IX_Users_NormalizedEmail");

        builder.HasIndex(user =>
                new
                {
                    user.IsActive,
                    user.LockoutEnd
                })
            .HasDatabaseName(
                "IX_Users_Active_LockoutEnd");
    }
}
