namespace SiPacul.Api.Common.Http;

public sealed class PublicResponseSecurityHeadersMiddleware(
    RequestDelegate next)
{
    public const string ContentSecurityPolicy =
        "base-uri 'self'; frame-ancestors 'none'; object-src 'none'";
    public const string ReferrerPolicy =
        "strict-origin-when-cross-origin";
    public const string XContentTypeOptions = "nosniff";
    public const string XFrameOptions = "DENY";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["Content-Security-Policy"] =
            ContentSecurityPolicy;
        context.Response.Headers["Referrer-Policy"] =
            ReferrerPolicy;
        context.Response.Headers["X-Content-Type-Options"] =
            XContentTypeOptions;
        context.Response.Headers["X-Frame-Options"] =
            XFrameOptions;

        await next(context);
    }
}
