using FluentValidation;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ShowRoom.Web.App;
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
    .AddInteractiveServerComponents();

// Typed backend clients (Refit over IHttpClientFactory) + the module facades the UI orchestrates.
builder.Services.AddBackendApis(builder.Configuration);
builder.Services.AddScoped<ICustomerFacade, CustomerFacade>();

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
