using System.Text.RegularExpressions;
using SiPacul.Domain.Common.Base;

namespace SiPacul.Domain.Entities.Organizations;

public sealed partial class Organization : AggregateRoot
{
    public const int MaxCodeLength = 30;

    public const int MaxNameLength = 150;

    public const int MaxLegalNameLength = 200;

    public const int MaxTimeZoneLength = 100;

    public const string DefaultTimeZone = "Asia/Jakarta";

    private Organization()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? LegalName { get; private set; }

    public string TimeZone { get; private set; } =
        DefaultTimeZone;

    public bool IsActive { get; private set; } = true;

    public static Organization Create(
        string code,
        string name,
        string? legalName = null,
        string? timeZone = null)
    {
        return new Organization
        {
            Code = NormalizeCode(code),
            Name = NormalizeName(name),
            LegalName = NormalizeLegalName(legalName),
            TimeZone = NormalizeTimeZone(timeZone)
        };
    }

    public void Update(
        string name,
        string? legalName,
        string? timeZone)
    {
        var normalizedName = NormalizeName(name);

        var normalizedLegalName =
            NormalizeLegalName(legalName);

        var normalizedTimeZone =
            NormalizeTimeZone(timeZone);

        if (Name == normalizedName &&
            LegalName == normalizedLegalName &&
            TimeZone == normalizedTimeZone)
        {
            return;
        }

        Name = normalizedName;
        LegalName = normalizedLegalName;
        TimeZone = normalizedTimeZone;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Organization code cannot be empty.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                $"Organization code cannot exceed " +
                $"{MaxCodeLength} characters.",
                nameof(code));
        }

        if (!OrganizationCodePattern()
            .IsMatch(normalizedCode))
        {
            throw new ArgumentException(
                "Organization code may only contain " +
                "letters, numbers, hyphens, and underscores.",
                nameof(code));
        }

        return normalizedCode;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Organization name cannot be empty.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Organization name cannot exceed " +
                $"{MaxNameLength} characters.",
                nameof(name));
        }

        return normalizedName;
    }

    private static string? NormalizeLegalName(
        string? legalName)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            return null;
        }

        var normalizedLegalName = legalName.Trim();

        if (normalizedLegalName.Length >
            MaxLegalNameLength)
        {
            throw new ArgumentException(
                $"Organization legal name cannot exceed " +
                $"{MaxLegalNameLength} characters.",
                nameof(legalName));
        }

        return normalizedLegalName;
    }

    private static string NormalizeTimeZone(
        string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return DefaultTimeZone;
        }

        var normalizedTimeZone = timeZone.Trim();

        if (normalizedTimeZone.Length >
            MaxTimeZoneLength)
        {
            throw new ArgumentException(
                $"Organization time zone cannot exceed " +
                $"{MaxTimeZoneLength} characters.",
                nameof(timeZone));
        }

        return normalizedTimeZone;
    }

    [GeneratedRegex("^[A-Z0-9_-]+$")]
    private static partial Regex OrganizationCodePattern();
}
