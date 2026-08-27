namespace SiPacul.Api.Security.RateLimiting;

public static class SiPaculRateLimitingDefaults
{
    public const string AuthenticationPolicyName =
        "authentication";

    public const string BootstrapPolicyName =
        "bootstrap";

    public const string RejectionCode =
        "RateLimit.Exceeded";

    public const int GlobalPermitLimit = 240;

    public const int AuthenticationPermitLimit = 10;

    public const int BootstrapPermitLimit = 5;

    public static TimeSpan Window =>
        TimeSpan.FromMinutes(1);
}
