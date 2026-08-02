namespace SiPacul.Application.Security.Authentication.Contracts;

public sealed record LoginRequest(
    string? Email,
    string? Password,
    bool RememberMe)
{
    public const int MaxEmailLength = 256;

    public const int MaxPasswordLength = 1024;
}
