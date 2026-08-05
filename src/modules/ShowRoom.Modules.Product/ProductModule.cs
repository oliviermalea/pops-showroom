using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks;
using ShowRoom.Modules.Product.Features.CreateProduct;
using ShowRoom.Modules.Product.Features.GetProductByPublicId;
using ShowRoom.Modules.Product.Features.GetProducts;

namespace ShowRoom.Modules.Product;

/// <summary>
/// Composition root for the Product module. Wires Infrastructure + Application, exposes routing
/// (behind a route group), and gates middleware behind the module feature flag. Database migration
/// runs through the secured <c>UseInfrastructure -&gt; UseDatabase</c> pipeline (no ad-hoc startup call).
/// </summary>
public static class ProductModule
{
    /// <summary>
    /// OpenTelemetry source name for the Product module.
    /// Register with <c>AddOpenTelemetry().WithTracing(t => t.AddSource(ProductModule.TelemetrySourceName))</c>.
    /// </summary>
    public const string TelemetrySourceName = "ShowRoom.Modules.Product";

    public static IHostApplicationBuilder AddProductModule(this IHostApplicationBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Services are always registered so routes and DI stay consistent;
        // the feature flag controls routing and middleware only.
        builder.AddInfrastructureModule();
        builder.Services.AddApplicationModule();

        return builder;
    }

    public static void RegisterProductModule(this WebApplication app, string module)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsModuleEnabled(module))
        {
            return;
        }

        app.UseProductModule();
    }

    public static IEndpointRouteBuilder MapProductModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.CreateProductGroup();

        // Public-facing product URLs always use PublicId values.
        group.MapCreateProduct();
        group.MapGetProducts();
        group.MapGetProductByPublicId();

        return endpoints;
    }

    private static IApplicationBuilder UseProductModule(this IApplicationBuilder applicationBuilder)
        => applicationBuilder.UseInfrastructure();

    private static RouteGroupBuilder CreateProductGroup(this IEndpointRouteBuilder endpoints)
        => endpoints
            .MapGroup(ProductConventions.BaseRoute)
            .WithTags(ProductConventions.Tag);
}
