using Asp.Versioning;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics.CodeAnalysis;

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

// Note: les validators FluentValidation sont enregistrés, mais ils ne sont pas exécutés automatiquement dans ce pipeline Minimal API.
//builder.Services.AddAcquisitionModule(builder.Configuration, ModulesRegistry.Acquisition);
//builder.Services.AddEditorialModule(builder.Configuration, ModulesRegistry.Editorial);

// Enregistre la source OTel du module Acquisition dans le pipeline tracing
//builder.Services.AddOpenTelemetry()
//    .WithTracing(tracing => tracing
//        .AddSource(AcquisitionModule.TelemetrySourceName)
//        .AddSource(EditorialModule.TelemetrySourceName));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

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

//versionedApi.MapAcquisitionModule();
//versionedApi.MapEditorialModule();

//app.RegisterAcquisitionModule(ModulesRegistry.Acquisition);
//app.RegisterEditorialModule(ModulesRegistry.Editorial);

app.Run();

/// <summary>
/// Parametrizes the serilog App logger. 
/// </summary>
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

    // Dev : lisibilité humaine
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