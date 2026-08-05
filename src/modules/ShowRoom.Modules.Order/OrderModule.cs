using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks;
using ShowRoom.Modules.Order.Features.CreateOrder;
using ShowRoom.Modules.Order.Features.GetOrderByPublicId;
using ShowRoom.Modules.Order.Features.GetOrders;

namespace ShowRoom.Modules.Order;

/// <summary>
/// Composition root for the Order module. Wires Infrastructure + Application, exposes routing (behind
/// a route group), and gates middleware behind the module feature flag. Database migration runs
/// through the secured <c>UseInfrastructure -&gt; UseDatabase</c> pipeline (no ad-hoc startup call).
/// </summary>
public static class OrderModule
{
    /// <summary>
    /// OpenTelemetry source name for the Order module.
    /// Register with <c>AddOpenTelemetry().WithTracing(t => t.AddSource(OrderModule.TelemetrySourceName))</c>.
    /// </summary>
    public const string TelemetrySourceName = "ShowRoom.Modules.Order";

    public static IHostApplicationBuilder AddOrderModule(this IHostApplicationBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Services are always registered so routes and DI stay consistent;
        // the feature flag controls routing and middleware only.
        builder.AddInfrastructureModule();
        builder.Services.AddApplicationModule();

        return builder;
    }

    public static void RegisterOrderModule(this WebApplication app, string module)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsModuleEnabled(module))
        {
            return;
        }

        app.UseOrderModule();
    }

    public static IEndpointRouteBuilder MapOrderModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.CreateOrderGroup();

        // Public-facing order URLs always use PublicId values.
        group.MapCreateOrder();
        group.MapGetOrders();
        group.MapGetOrderByPublicId();

        return endpoints;
    }

    private static IApplicationBuilder UseOrderModule(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseInfrastructure();

    private static RouteGroupBuilder CreateOrderGroup(this IEndpointRouteBuilder endpoints)
        => endpoints
            .MapGroup(OrderConventions.BaseRoute)
            .WithTags(OrderConventions.Tag);
}
