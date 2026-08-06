using Asp.Versioning;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ShowRoom.Business.Api.Modules;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Order;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.Modules.Order.Features.Messaging;
using ShowRoom.Modules.Order.Persistence;
using ShowRoom.Modules.Product;
using System.Diagnostics.CodeAnalysis;
using Wolverine;
using Wolverine.RabbitMQ;

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

// Modules registration (modulith composition root). Services are always registered;
// the feature flag gates routing and middleware.
builder.AddCustomerModule(ModulesRegistry.Customer);
builder.AddOrderModule(ModulesRegistry.Order);
builder.AddProductModule(ModulesRegistry.Product);

// Messaging (AMQP / RabbitMQ via Wolverine). Machine-to-machine calls between modules go through the
// broker with request/reply (IMessageBus.InvokeAsync) — never HTTP. DisableConventionalLocalRouting
// forces the request onto RabbitMQ even though the modules currently share one process, which is the
// seam that lets the Order module be extracted into its own service later.
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();

    opts.Policies.DisableConventionalLocalRouting();

    // EF Core's AddDbContext uses a factory registration Wolverine's codegen can't inline; route the
    // DbContext through the service locator (Wolverine 6 forbids implicit service location by default).
    opts.CodeGeneration.AlwaysUseServiceLocationFor<OrdersContext>();

    opts.PublishMessage<GetOrdersForCustomer>()
        .ToRabbitQueue(OrderMessagingContract.GetOrdersForCustomerQueue);
    opts.ListenToRabbitQueue(OrderMessagingContract.GetOrdersForCustomerQueue);

    // Only the Order module's message handler is a Wolverine handler; the modules' REPR
    // IQueryHandler/ICommandHandler types use a different (non-Wolverine) convention and must not be
    // scanned as message handlers.
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeType(typeof(GetOrdersForCustomerMessageHandler));
});

// Enregistre les sources OTel des modules dans le pipeline tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(CustomerModule.TelemetrySourceName)
        .AddSource(OrderModule.TelemetrySourceName)
        .AddSource(ProductModule.TelemetrySourceName));

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

// Module middleware, gated by the module feature flag.
app.RegisterCustomerModule(ModulesRegistry.Customer);
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
versionedApi.MapCustomerModule();
versionedApi.MapOrderModule();
versionedApi.MapProductModule();

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