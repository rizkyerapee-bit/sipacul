namespace SiPacul.Infrastructure.Identity;

public sealed class FirstOwnerBootstrapOptions
{
    public const string SectionName =
        "Bootstrap";

    public const int MinimumTokenLength = 32;

    public string? OwnerToken { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(OwnerToken) &&
        OwnerToken.Length >= MinimumTokenLength;
}
