namespace SiPacul.Application.Security.Authentication.Contracts;

public sealed record AntiforgeryTokenResponse(
    string RequestToken,
    string HeaderName);
