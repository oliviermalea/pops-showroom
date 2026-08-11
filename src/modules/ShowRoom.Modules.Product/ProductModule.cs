using System.Diagnostics;
using System.Diagnostics.Metrics;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Product.Features.CreateProduct;
using ShowRoom.Modules.Product.Features.GetProductByPublicId;
using ShowRoom.Modules.Product.Features.GetProducts;
using ShowRoom.Modules.Product.Persistence;

namespace ShowRoom.Modules.Product;

/// <summary>
/// Composition root and identity of the Product module: naming/routing conventions, its OpenTelemetry
/// source, and the Add/Register/Map wiring. Infrastructure (EF, validators, time) is wired privately.
/// This module has no machine-to-machine messaging.
/// </summary>
public static class ProductModule
{
    public const string ModuleName = "Product";
    public const string Tag = ModuleName;
    public const string RouteSegment = "products";
    public const string BaseRoute = "/" + RouteSegment;

    /// <summary>OpenTelemetry source name; register with <c>AddSource(ProductModule.TelemetrySourceName)</c>.</summary>
    public const string TelemetrySourceName = "ShowRoom.Modules.Product";

    /// <summary>Module ActivitySource; feature handlers open their spans from here.</summary>
    internal static readonly ActivitySource ActivitySource = new(TelemetrySourceName);

    /// <summary>Module Meter for business metrics; register with <c>AddMeter(ProductModule.TelemetrySourceName)</c>.</summary>
    internal static readonly Meter Meter = new(TelemetrySourceName);

    public static string BuildApiBasePath(ApiVersion? version)
        => version is not null ? $"/api/v{version}{BaseRoute}" : $"/api{BaseRoute}";

    public static IHostApplicationBuilder AddProductModule(this IHostApplicationBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddInfrastructure();
        builder.Services.AddApplicationHandlersFromAssembly(typeof(ProductModule).Assembly);

        return builder;
    }

    public static void RegisterProductModule(this WebApplication app, string module)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsModuleEnabled(module))
        {
            return;
        }

        app.UseDatabase();
    }

    public static IEndpointRouteBuilder MapProductModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(BaseRoute).WithTags(Tag);

        // Public-facing product URLs always use PublicId values.
        group.MapCreateProduct();
        group.MapGetProducts();
        group.MapGetProductByPublicId();

        return endpoints;
    }

    private static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabase(builder.Configuration);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        builder.Services.AddValidatorsFromAssembly(typeof(ProductModule).Assembly, includeInternalTypes: true);
    }
}
