using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;
using ShowRoom.Modules.Customer.Persistence;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer;

/// <summary>
/// Composition root for the Customer module. Wires Infrastructure + Application, exposes routing
/// (behind a route group), and gates middleware behind the module feature flag.
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

    /// <summary>Applies pending migrations and seeds a demo customer. Intended for local/dev use only.</summary>
    public static async Task InitializeCustomerModuleAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomersContext>();

        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Customers.AnyAsync())
        {
            // Stable public id for local smoke-testing: cus_00...0ad
            var demoPublicId = PublicId.Parse("cus_" + new string('0', 30) + "ad");

            var customer = CustomerAggregate.Restore(
                CustomerId.FromGuid(Guid.CreateVersion7()),
                demoPublicId,
                firstName: "Ada",
                lastName: "Lovelace",
                email: Email.Create("ada.lovelace@example.com").Value,
                phone: PhoneNumber.Create("+33123456789").Value,
                status: CustomerStatus.Active,
                createdAt: DateTimeOffset.UtcNow,
                updatedAt: null);

            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync();
        }
    }
}
