using SiPacul.Infrastructure.Identity;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Identity;

public sealed class ApplicationUserTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldCreateActiveUser()
    {
        var before = DateTime.UtcNow;

        var user =
            ApplicationUser.Create(
                "owner@example.com");

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.True(user.IsActive);

        Assert.Equal(
            "owner@example.com",
            user.Email);

        Assert.Equal(
            "owner@example.com",
            user.UserName);

        Assert.False(
            string.IsNullOrWhiteSpace(
                user.SecurityStamp));

        Assert.InRange(
            user.CreatedAt,
            before,
            after);

        Assert.Null(user.LastLoginAt);
    }

    [Fact]
    public void Create_ShouldTrimAndLowercaseEmail()
    {
        var user =
            ApplicationUser.Create(
                "  Owner@Example.COM  ");

        Assert.Equal(
            "owner@example.com",
            user.Email);

        Assert.Equal(
            "owner@example.com",
            user.UserName);
    }

    [Fact]
    public void Create_WithBlankEmail_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ApplicationUser.Create("   "));
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ApplicationUser.Create(
                "not-an-email"));
    }

    [Fact]
    public void Create_WithLongEmail_ShouldThrow()
    {
        var email =
            new string('a', 250) +
            "@example.com";

        Assert.Throws<ArgumentException>(() =>
            ApplicationUser.Create(email));
    }

    [Fact]
    public void Deactivate_ShouldMakeUserInactive()
    {
        var user =
            ApplicationUser.Create(
                "owner@example.com");

        user.Deactivate();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Activate_ShouldMakeUserActive()
    {
        var user =
            ApplicationUser.Create(
                "owner@example.com");

        user.Deactivate();
        user.Activate();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldSetUtcTimestamp()
    {
        var user =
            ApplicationUser.Create(
                "owner@example.com");

        var before = DateTime.UtcNow;

        user.RecordSuccessfulLogin();

        var after = DateTime.UtcNow;

        Assert.NotNull(user.LastLoginAt);

        Assert.InRange(
            user.LastLoginAt!.Value,
            before,
            after);

        Assert.Equal(
            DateTimeKind.Utc,
            user.LastLoginAt.Value.Kind);
    }
}
