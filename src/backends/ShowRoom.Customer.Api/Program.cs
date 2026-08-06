using Asp.Versioning;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
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
Log.Logger = CreateSerilogAppLogger();
Log.Information("Starting up");
Log.Information("Launching {ServiceName} app", serviceName);
Log.Information("Environment: {Environment}", env.EnvironmentName);
builder.Host.UseSerilog();
//// End Logs

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
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
        .AddMeter(WolverineObservability.MeterNamePattern));

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error");
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

/// <summary>Parametrizes the serilog App logger.</summary>
/// <returns><see cref="ILogger"/></returns>
static Serilog.ILogger CreateSerilogAppLogger()
{
    var commonEnrichment = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("Wolverine", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ShowRoom")
        .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown");

    var devOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Module}] [{Feature}] [{RequestId}] | {Message:lj}{NewLine}{Exception}";

    return commonEnrichment
        .WriteTo.Console(
            outputTemplate: devOutputTemplate,
            theme: AnsiConsoleTheme.Code)
        .WriteTo.File(
            path: $"logs/log-{DateTime.Now:yyyy-MM-dd}.txt",
            outputTemplate: devOutputTemplate,
            fileSizeLimitBytes: 10_000_000,
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: 7)
        .CreateBootstrapLogger();
}

[ExcludeFromCodeCoverage]
public partial class Program
{
    protected Program() { }
}
