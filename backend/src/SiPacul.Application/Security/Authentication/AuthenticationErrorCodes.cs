namespace SiPacul.Application.Security.Authentication;

public static class AuthenticationErrorCodes
{
    public const string InvalidRequest =
        "Authentication.InvalidRequest";

    public const string InvalidCredentials =
        "Authentication.InvalidCredentials";

    public const string InvalidAntiforgeryToken =
        "Authentication.InvalidAntiforgeryToken";

    public const string Unauthenticated =
        "Authentication.Unauthenticated";
}
