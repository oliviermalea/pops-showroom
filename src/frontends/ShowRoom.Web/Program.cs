using System.IO.Compression;
using FluentValidation;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ShowRoom.Web.App;
using ShowRoom.Web.Client;
using ShowRoom.Web.Client.Infrastructure.Api;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Infrastructure.Api;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var serviceName = env.ApplicationName;

//// Logs
Log.Logger = ConfigureAppLogger(new LoggerConfiguration()).CreateBootstrapLogger();
Log.Information("Starting up");
Log.Information("Launching {ServiceName} app", serviceName);
Log.Information("Environment: {Environment}", env.EnvironmentName);
// writeToProviders: true forwards Serilog events to the registered ILoggerProviders — including the
// OpenTelemetry logging provider wired by AddServiceDefaults — so logs are exported over OTLP and appear
// in the Aspire dashboard "Structured Logs".
builder.Host.UseSerilog(
    (context, loggerConfiguration) => ConfigureAppLogger(loggerConfiguration),
    preserveStaticLogger: true,
    writeToProviders: true);
//// End Logs

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Typed backend clients (Refit over IHttpClientFactory) + the module facades the UI orchestrates.
builder.Services.AddBackendApis(builder.Configuration);
builder.Services.AddScoped<ICustomerFacade, CustomerFacade>();

// The catalogue screens run in WebAssembly, but they are PRERENDERED here first: the same components
// execute once on this server before the runtime reaches the browser, so their dependencies must exist
// on this side too. Same registration method as the client's Program.cs — one place, no drift.
builder.Services.AddCatalog(builder.Configuration);

// Response compression of the DYNAMIC responses (the SSR HTML). Static assets are already served
// pre-compressed and fingerprinted by MapStaticAssets, so they are deliberately NOT re-compressed here.
//
// EnableForHttps is opt-in: compressing over TLS re-opens the BREACH class of attacks when a response
// mixes a secret with attacker-controlled input. These pages carry no secret and no session token, so
// the trade is acceptable — it must be re-examined the day authentication lands.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json", "image/svg+xml"]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);

// NO output cache here, and it is a deliberate choice — measured, not assumed:
//  * the customer screens must not be cached at all (see the no-store policy below): a cached list
//    would hide the customer that was just created;
//  * the home page, the only cacheable candidate, carries an antiforgery `Set-Cookie` like every
//    Razor Components SSR response. The output cache legitimately refuses to store such a response,
//    and forcing it would hand one visitor's antiforgery token to every other visitor.
// Serving it from a cache would take a per-request render of ~30 ms off the server — not worth a
// cross-visitor token leak. Revisit only behind a CDN/proxy that strips the cookie.

// Form validation: FluentValidation only (never DataAnnotations), scoped like the components using it.
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// First in the pipeline: it wraps everything downstream, including what comes out of the output cache
// (the cache therefore stores uncompressed bytes, compressed once per client encoding).
app.UseResponseCompression();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// Customer data must never be stored by a browser or an intermediary proxy: a "back" after logout (or a
// shared machine) would replay it. Applied by path rather than per component, because a component
// declaring [StreamRendering] has already flushed its headers by the time it renders.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/customers"))
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    }

    await next(context);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    // Without this, the routes declared in the client assembly simply do not exist: the router only
    // scans the assemblies it is told about, and a missing one shows up as a 404, not as an error.
    .AddAdditionalAssemblies(ClientAssembly.Value);

app.Run();

/// <summary>
/// Applies the ShowRoom Serilog configuration (levels, enrichment, Console + File sinks) to the given
/// <see cref="LoggerConfiguration"/>. Shared by the bootstrap logger and the host logger so both behave
/// identically; OTLP export to the Aspire dashboard is handled by the OpenTelemetry logging provider via
/// <c>writeToProviders: true</c>, not by a Serilog sink.
/// </summary>
static LoggerConfiguration ConfigureAppLogger(LoggerConfiguration loggerConfiguration)
{
    // Dev : lisibilité humaine
    var devOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Module}] [{Feature}] [{RequestId}] | {Message:lj}{NewLine}{Exception}";

    return loggerConfiguration
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ShowRoom")
        .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown")
        .WriteTo.Console(
            outputTemplate: devOutputTemplate,
            theme: AnsiConsoleTheme.Code)
        .WriteTo.File(
            path: $"logs/log-{DateTime.Now:yyyy-MM-dd}.txt",
            outputTemplate: devOutputTemplate,
            fileSizeLimitBytes: 10_000_000,
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: 7);
}
