namespace SiPacul.Api.Security;

public static class SiPaculAuthenticationDefaults
{
    public const string AuthenticationCookieName =
        "__Host-SiPacul.Auth";

    public const string AntiforgeryCookieName =
        "__Host-SiPacul.Csrf";

    public const string AntiforgeryHeaderName =
        "X-CSRF-TOKEN";

    public static readonly TimeSpan CookieLifetime =
        TimeSpan.FromHours(8);

    public static readonly TimeSpan LockoutDuration =
        TimeSpan.FromMinutes(15);

    public static readonly TimeSpan
        SecurityStampValidationInterval =
            TimeSpan.FromMinutes(5);
}
