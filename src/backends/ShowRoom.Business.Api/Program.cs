using Asp.Versioning;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Observability;
using ShowRoom.Business.Api.Cors;
using ShowRoom.Business.Api.Modules;
using ShowRoom.Modules.Order;
using ShowRoom.Modules.Product;
using System.Diagnostics.CodeAnalysis;
using Wolverine;

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
// in the Aspire dashboard "Structured Logs" (Serilog otherwise writes only to its own Console/File sinks).
builder.Host.UseSerilog(
    (context, loggerConfiguration) => ConfigureAppLogger(loggerConfiguration),
    preserveStaticLogger: true,
    writeToProviders: true);
//// End Logs

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
// Uniform RFC 7807 error handling: a malformed request body becomes a clean 400 ProblemDetails
// (see BadRequestExceptionHandler) instead of a raw stack trace / opaque 500.
builder.Services.AddShowRoomProblemDetails();
builder.Services.AddFeatureManagement();
// The WebAssembly catalogue screens call this API from the browser: it must name the origins
// it accepts (see BrowserClientsCors).
builder.Services.AddBrowserClientsCors(builder.Configuration);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// This service hosts the Order and Product bounded contexts. Customer lives in ShowRoom.Customer.Api
// and reaches Order over the message bus.
builder.AddOrderModule(ModulesRegistry.Order);
builder.AddProductModule(ModulesRegistry.Product);

// Messaging (AMQP / RabbitMQ via Wolverine). The transport is configured centrally from the "Messaging"
// section; the Order module contributes its listener + message handler via an IWolverineExtension
// registered in AddOrderModule (see Messaging/MessagingModule), so this host stays agnostic of module
// specifics. The request originates in ShowRoom.Customer.Api, so it genuinely crosses RabbitMQ.
builder.Host.UseWolverine(opts => opts.ConfigureShowRoomMessaging(builder.Configuration));

// Observability: module traces + Wolverine messaging spans + native RabbitMQ.Client AMQP spans, plus
// Wolverine metrics. Trace context propagated over the broker makes the consumer span here join the
// Customer service's trace.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(OrderModule.TelemetrySourceName)
        .AddSource(ProductModule.TelemetrySourceName)
        .AddSource(WolverineObservability.ActivitySourceName)
        .AddSource(WolverineObservability.RabbitMqPublisherSourceName)
        .AddSource(WolverineObservability.RabbitMqSubscriberSourceName))
    .WithMetrics(metrics => metrics
        .AddMeter(WolverineObservability.MeterNamePattern)
        .AddMeter(MessagingMetrics.MeterName)
        .AddMeter(OrderModule.TelemetrySourceName)
        .AddMeter(ProductModule.TelemetrySourceName));

var app = builder.Build();

app.MapDefaultEndpoints();

// Uniform exception handling (all environments). Registered before the endpoints so the
// BadRequestExceptionHandler turns a malformed body into a clean 400 ProblemDetails and the request
// never reaches the developer exception page.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Before any endpoint: a preflight OPTIONS must be answered by the CORS middleware, not routed.
app.UseBrowserClientsCors();

// Module middleware, gated by the module feature flag.
app.RegisterOrderModule(ModulesRegistry.Order);
app.RegisterProductModule(ModulesRegistry.Product);

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var api = app.MapGroup("/api")
    .WithTags(serviceName);

api.MapGet("/status", () => Results.Ok(new
{
    Service = serviceName,
    Status = "OK",
    Timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetApiStatus");

var versionedApi = api.MapGroup("/v{version:apiVersion}")
    .WithApiVersionSet(versionSet);

// Modules endpoints (mapped under /api/v{version}). Routes are always mapped so DI/routing stay
// consistent; the feature flag gates module middleware via RegisterXModule.
versionedApi.MapOrderModule();
versionedApi.MapProductModule();

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
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("Wolverine", LogEventLevel.Information)
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

[ExcludeFromCodeCoverage]
public partial class Program
{
    protected Program() { }
}