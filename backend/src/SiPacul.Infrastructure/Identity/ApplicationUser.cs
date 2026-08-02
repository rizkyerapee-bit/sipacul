using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace SiPacul.Infrastructure.Identity;

public sealed class ApplicationUser :
    IdentityUser<Guid>
{
    public const int MaxEmailLength = 256;

    private ApplicationUser()
    {
    }

    public DateTime CreatedAt { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public bool IsActive { get; private set; }

    public static ApplicationUser Create(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            SecurityStamp =
                Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeEmail(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email cannot be empty.",
                nameof(email));
        }

        var normalized =
            email.Trim().ToLowerInvariant();

        if (normalized.Length > MaxEmailLength)
        {
            throw new ArgumentException(
                $"Email cannot exceed " +
                $"{MaxEmailLength} characters.",
                nameof(email));
        }

        if (!MailAddress.TryCreate(
                normalized,
                out var parsedAddress) ||
            parsedAddress.Address != normalized)
        {
            throw new ArgumentException(
                "Email format is invalid.",
                nameof(email));
        }

        return normalized;
    }
}
