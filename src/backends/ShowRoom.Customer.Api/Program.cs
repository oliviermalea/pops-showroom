using Asp.Versioning;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Observability;
using ShowRoom.Customer.Api.Modules;
using ShowRoom.Modules.Customer;
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
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// This service hosts only the Customer bounded context.
builder.AddCustomerModule(ModulesRegistry.Customer);

// Messaging (AMQP / RabbitMQ via Wolverine). The transport is configured centrally from the "Messaging"
// section; the Customer module contributes its own outbound routes via an IWolverineExtension registered
// in AddCustomerModule (see Messaging/MessagingModule), so this host stays agnostic of module specifics.
builder.Host.UseWolverine(opts => opts.ConfigureShowRoomMessaging(builder.Configuration));

// Observability: Customer module traces + Wolverine messaging spans + native RabbitMQ.Client AMQP
// spans, plus Wolverine metrics. Wolverine propagates the W3C trace context across the broker, so the
// consumer service's spans join this service's trace end to end.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(CustomerModule.TelemetrySourceName)
        .AddSource(WolverineObservability.ActivitySourceName)
        .AddSource(WolverineObservability.RabbitMqPublisherSourceName)
        .AddSource(WolverineObservability.RabbitMqSubscriberSourceName))
    .WithMetrics(metrics => metrics
        .AddMeter(WolverineObservability.MeterNamePattern)
        .AddMeter(MessagingMetrics.MeterName)
        .AddMeter(CustomerModule.TelemetrySourceName));

var app = builder.Build();

app.MapDefaultEndpoints();

// Uniform exception handling (all environments). Registered before the endpoints so the
// BadRequestExceptionHandler turns a malformed body into a clean 400 ProblemDetails and the request
// never reaches the developer exception page.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Module middleware (migrations, etc.), gated by the module feature flag.
app.RegisterCustomerModule(ModulesRegistry.Customer);

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

versionedApi.MapCustomerModule();

app.Run();

/// <summary>
/// Applies the ShowRoom Serilog configuration (levels, enrichment, Console + File sinks) to the given
/// <see cref="LoggerConfiguration"/>. Shared by the bootstrap logger and the host logger so both behave
/// identically; OTLP export to the Aspire dashboard is handled by the OpenTelemetry logging provider via
/// <c>writeToProviders: true</c>, not by a Serilog sink.
/// </summary>
static LoggerConfiguration ConfigureAppLogger(LoggerConfiguration loggerConfiguration)
{
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
