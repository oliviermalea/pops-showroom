using System.Diagnostics;
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
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;
using ShowRoom.Modules.Customer.Features.GetCustomers;
using ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;
using ShowRoom.Modules.Customer.Persistence;
using Wolverine;

namespace ShowRoom.Modules.Customer;

/// <summary>
/// Composition root and identity of the Customer module: naming/routing conventions, its OpenTelemetry
/// source, and the Add/Register/Map wiring. Infrastructure (EF, messaging, the outbound Order gateway,
/// validators, time) is wired privately; each feature slice contributes its own messaging routes via an
/// <see cref="IWolverineExtension"/> discovered here.
/// </summary>
public static class CustomerModule
{
    public const string ModuleName = "Customer";
    public const string Tag = ModuleName;
    public const string RouteSegment = "customers";
    public const string BaseRoute = "/" + RouteSegment;

    /// <summary>OpenTelemetry source name; register with <c>AddSource(CustomerModule.TelemetrySourceName)</c>.</summary>
    public const string TelemetrySourceName = "ShowRoom.Modules.Customer";

    /// <summary>Module ActivitySource; feature handlers open their spans from here.</summary>
    internal static readonly ActivitySource ActivitySource = new(TelemetrySourceName);

    public static string BuildApiBasePath(ApiVersion? version)
        => version is not null ? $"/api/v{version}{BaseRoute}" : $"/api{BaseRoute}";

    public static IHostApplicationBuilder AddCustomerModule(this IHostApplicationBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddInfrastructure();
        builder.Services.AddApplicationHandlersFromAssembly(typeof(CustomerModule).Assembly);

        return builder;
    }

    public static void RegisterCustomerModule(this WebApplication app, string module)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsModuleEnabled(module))
        {
            return;
        }

        app.UseDatabase();
    }

    public static IEndpointRouteBuilder MapCustomerModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(BaseRoute).WithTags(Tag);

        // Public-facing customer URLs always use PublicId values.
        group.MapCreateCustomer();
        group.MapGetCustomers();
        group.MapGetCustomerWithOrders();
        group.MapGetCustomerByPublicId();

        return endpoints;
    }

    private static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddMessaging(builder.Configuration);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Outbound anti-corruption gateway to the Order service (AMQP request/reply, not HTTP).
        builder.Services.AddScoped<IOrderHistory, MessagingOrderHistory>();

        builder.Services.AddValidatorsFromAssembly(typeof(CustomerModule).Assembly, includeInternalTypes: true);
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        if (!MessagingOptions.FromConfiguration(configuration).Enabled)
        {
            return services;
        }

        services.Scan(scan => scan
            .FromAssemblies(typeof(CustomerModule).Assembly)
            .AddClasses(classes => classes.AssignableTo<IWolverineExtension>(), publicOnly: false)
            .As<IWolverineExtension>()
            .WithSingletonLifetime());

        return services;
    }
}
