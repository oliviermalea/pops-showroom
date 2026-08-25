using Microsoft.Extensions.Options;

namespace ShowRoom.Business.Api.Cors;

/// <summary>Origins allowed to call this API straight from a browser, bound from the <c>Cors</c> section.</summary>
public sealed class BrowserClientsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Exact origins (scheme + host + port). Empty by default: an unconfigured environment allows no
    /// browser origin at all, rather than silently opening up.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}

/// <summary>
/// CORS for the WebAssembly catalogue screens, which run <b>in the browser</b> and therefore call this
/// API directly — unlike the server-rendered screens, whose calls leave from the web host.
/// </summary>
/// <remarks>
/// <para>This is the visible cost of the WASM render mode: the browser becomes a first-class client of
/// the API, so the API must name the origins it accepts. Keep it least-privilege — the catalogue only
/// reads, so only <c>GET</c> is allowed, and credentials are never enabled.</para>
///
/// <para>Origins come from configuration (Aspire injects them), never from source: a wildcard or a
/// hard-coded host would outlive the demo it was written for.</para>
/// </remarks>
internal static class BrowserClientsCors
{
    internal const string PolicyName = "browser-clients";

    internal static IServiceCollection AddBrowserClientsCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BrowserClientsOptions>(configuration.GetSection(BrowserClientsOptions.SectionName));

        var options = configuration.GetSection(BrowserClientsOptions.SectionName).Get<BrowserClientsOptions>()
            ?? new BrowserClientsOptions();

        services.AddCors(cors => cors.AddPolicy(PolicyName, policy => policy
            .WithOrigins(options.AllowedOrigins)
            .WithMethods(HttpMethods.Get)
            .AllowAnyHeader()));

        return services;
    }

    /// <summary>
    /// Plugs the policy in, and says so in the logs. A CORS failure is invisible server-side — the
    /// request succeeds and the browser discards the answer — so the configured origins must be stated
    /// at startup, otherwise diagnosing it means guessing.
    /// </summary>
    internal static WebApplication UseBrowserClientsCors(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<BrowserClientsOptions>>().Value;

        if (options.AllowedOrigins.Length == 0)
        {
            app.Logger.LogInformation(
                "CORS: no browser origin configured ({Section}:AllowedOrigins) — direct browser calls will be refused",
                BrowserClientsOptions.SectionName);

            return app;
        }

        app.Logger.LogInformation(
            "CORS: browser origins allowed for GET — {Origins}",
            string.Join(", ", options.AllowedOrigins));

        app.UseCors(PolicyName);

        return app;
    }
}
