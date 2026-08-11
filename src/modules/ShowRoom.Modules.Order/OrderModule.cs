using System.Diagnostics;
using System.Diagnostics.Metrics;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Order.Features.CreateOrder;
using ShowRoom.Modules.Order.Features.GetOrderByPublicId;
using ShowRoom.Modules.Order.Features.GetOrders;
using ShowRoom.Modules.Order.Persistence;
using Wolverine;

namespace ShowRoom.Modules.Order;

/// <summary>
/// Composition root and identity of the Order module: naming/routing conventions, its OpenTelemetry
/// source, and the Add/Register/Map wiring. Infrastructure (EF, messaging, validators, time) is wired
/// privately; each feature slice contributes its own messaging routes/handlers via an
/// <see cref="IWolverineExtension"/> discovered here.
/// </summary>
public static class OrderModule
{
    public const string ModuleName = "Order";
    public const string Tag = ModuleName;
    public const string RouteSegment = "orders";
    public const string BaseRoute = "/" + RouteSegment;

    /// <summary>OpenTelemetry source name; register with <c>AddSource(OrderModule.TelemetrySourceName)</c>.</summary>
    public const string TelemetrySourceName = "ShowRoom.Modules.Order";

    /// <summary>Module ActivitySource; feature handlers open their spans from here.</summary>
    internal static readonly ActivitySource ActivitySource = new(TelemetrySourceName);

    /// <summary>Module Meter for business metrics; register with <c>AddMeter(OrderModule.TelemetrySourceName)</c>.</summary>
    internal static readonly Meter Meter = new(TelemetrySourceName);

    public static string BuildApiBasePath(ApiVersion? version)
        => version is not null ? $"/api/v{version}{BaseRoute}" : $"/api{BaseRoute}";

    public static IHostApplicationBuilder AddOrderModule(this IHostApplicationBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Services are always registered so routes and DI stay consistent; the feature flag only gates
        // routing and middleware.
        builder.AddInfrastructure();
        builder.Services.AddApplicationHandlersFromAssembly(typeof(OrderModule).Assembly);

        return builder;
    }

    public static void RegisterOrderModule(this WebApplication app, string module)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsModuleEnabled(module))
        {
            return;
        }

        app.UseDatabase();
    }

    public static IEndpointRouteBuilder MapOrderModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(BaseRoute).WithTags(Tag);

        // Public-facing order URLs always use PublicId values.
        group.MapCreateOrder();
        group.MapGetOrders();
        group.MapGetOrderByPublicId();

        return endpoints;
    }

    private static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddMessaging(builder.Configuration);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        builder.Services.AddValidatorsFromAssembly(typeof(OrderModule).Assembly, includeInternalTypes: true);
    }

    // Each feature slice declares its Wolverine routes/listeners as an IWolverineExtension; they are
    // discovered here and applied by Wolverine at bootstrap. Gated by the Messaging switch, exactly like
    // AddDatabase for persistence.
    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        if (!MessagingOptions.FromConfiguration(configuration).Enabled)
        {
            return services;
        }

        services.Scan(scan => scan
            .FromAssemblies(typeof(OrderModule).Assembly)
            .AddClasses(classes => classes.AssignableTo<IWolverineExtension>(), publicOnly: false)
            .As<IWolverineExtension>()
            .WithSingletonLifetime());

        return services;
    }
}
