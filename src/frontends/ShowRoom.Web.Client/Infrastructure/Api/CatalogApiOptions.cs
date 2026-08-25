namespace ShowRoom.Web.Client.Infrastructure.Api;

/// <summary>
/// Address of the API the catalogue screens call, bound from the <c>BackendApi</c> section.
/// </summary>
/// <remarks>
/// <para>Unlike the server-rendered screens, this value is resolved <b>in the browser</b>, so it must be
/// an absolute, publicly reachable URL: an Aspire service-discovery scheme
/// (<c>https+http://showroom-business-api</c>) means nothing there. That is the concrete price of the
/// WebAssembly render mode — the API address stops being an internal detail.</para>
///
/// <para>In the browser the value comes from <c>wwwroot/appsettings.json</c>, served as a static file
/// and therefore fixed at build time. A real deployment would serve it from an endpoint instead; for a
/// local demo whose ports are pinned by the AppHost, a static value is enough and honest.</para>
/// </remarks>
public sealed class CatalogApiOptions
{
    public const string SectionName = "BackendApi";

    /// <summary>Base address of <c>ShowRoom.Business.Api</c>, which serves the Product surface.</summary>
    public required string BusinessApiBaseUrl { get; init; }

    /// <summary>API version segment used when building routes (<c>/api/v{version}/...</c>).</summary>
    public int ApiVersion { get; init; } = 1;
}
