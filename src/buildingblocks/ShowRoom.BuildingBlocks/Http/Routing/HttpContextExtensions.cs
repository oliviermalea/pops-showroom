using Asp.Versioning;
using Microsoft.AspNetCore.Http;

// Intentionally in the global namespace (matches the BuildingBlocks HTTP routing helpers).
public static class HttpContextExtensions
{
    /// <summary>Extracts the scheme, host and requested API version needed to build absolute URLs.</summary>
    public static UrlPart GetUrlPart(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scheme = context.Request.IsHttps ? "https" : "http";
        var host = context.Request.Host;
        var version = context.Features.Get<IApiVersioningFeature>()?.RequestedApiVersion;

        return new UrlPart(scheme, host, version);
    }
}
