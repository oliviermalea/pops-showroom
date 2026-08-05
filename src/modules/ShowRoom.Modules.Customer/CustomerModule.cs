using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks;
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

namespace ShowRoom.Modules.Customer;

/// <summary>
/// Composition root for the Customer module. Wires Infrastructure + Application, exposes routing
/// (behind a route group), and gates middleware behind the module feature flag. Database migration
/// runs through the secured <c>UseInfrastructure -&gt; UseDatabase</c> pipeline (no ad-hoc startup call).
/// </summary>
public static class CustomerModule
{
    /// <summary>
    /// OpenTelemetry source name for the Customer module.
    /// Register with <c>AddOpenTelemetry().WithTracing(t => t.AddSource(CustomerModule.TelemetrySourceName))</c>.
    /// </summary>
    public const string TelemetrySourceName = "ShowRoom.Modules.Customer";

    public static IHostApplicationBuilder AddCustomerModule(this IHostApplicationBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Services are always registered so routes and DI stay consistent;
        // the feature flag controls routing and middleware only.
        builder.AddInfrastructureModule();
        builder.Services.AddApplicationModule();

        return builder;
    }

    public static void RegisterCustomerModule(this WebApplication app, string module)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsModuleEnabled(module))
        {
            return;
        }

        app.UseCustomerModule();
    }

    public static IEndpointRouteBuilder MapCustomerModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.CreateCustomerGroup();

        // Public-facing customer URLs always use PublicId values.
        group.MapCreateCustomer();
        group.MapGetCustomerByPublicId();

        return endpoints;
    }

    private static IApplicationBuilder UseCustomerModule(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseInfrastructure();

    private static RouteGroupBuilder CreateCustomerGroup(this IEndpointRouteBuilder endpoints)
        => endpoints
            .MapGroup(CustomerConventions.BaseRoute)
            .WithTags(CustomerConventions.Tag);
}
