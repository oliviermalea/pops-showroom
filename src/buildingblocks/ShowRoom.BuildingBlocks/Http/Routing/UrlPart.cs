using Asp.Versioning;
using Microsoft.AspNetCore.Http;

// Intentionally in the global namespace (matches the BuildingBlocks HTTP routing helpers).
public sealed class UrlPart
{
    internal UrlPart(string scheme, HostString host, ApiVersion? version = null)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        Scheme = scheme;
        Host = host;
        Version = version;
    }

    public string Scheme { get; }

    public HostString Host { get; }

    public ApiVersion? Version { get; }
}
