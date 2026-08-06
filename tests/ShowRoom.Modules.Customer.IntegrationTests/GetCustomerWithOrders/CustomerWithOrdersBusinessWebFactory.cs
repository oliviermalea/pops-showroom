namespace ShowRoom.Modules.Customer.IntegrationTests.GetCustomerWithOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.Modules.Order.Persistence;
using ShowRoom.Testing;
using Testcontainers.RabbitMq;
using Wolverine;

/// <summary>
/// End-to-end factory for the Customer→Order messaging round-trip. Unlike the other test factories it
/// does NOT stub Wolverine's transports: it provides a real RabbitMQ broker, enables BOTH the Customer
/// and Order modules, and migrates both contexts into the shared Postgres container — so a call to
/// <c>GET /customers/{id}/with-orders</c> genuinely travels Customer → RabbitMQ → Order → reply.
/// </summary>
public sealed class CustomerWithOrdersBusinessWebFactory : BusinessWebFactory
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management")
        .Build();

    protected override bool StubExternalTransports => false;

    // Real broker + both modules enabled. Applied as host configuration on top of the appsettings json
    // (so the real broker connection string wins over the stubbed default used elsewhere).
    protected override IDictionary<string, string?> HostConfigurationOverrides()
        => new Dictionary<string, string?>
        {
            ["FeatureManagement:Customer"] = "true",
            ["FeatureManagement:Order"] = "true",
            ["ConnectionStrings:messaging"] = _rabbitMq.GetConnectionString(),
            ["ConnectionStrings:showroom-business"] = _postgreSqlContainer.GetConnectionString(),
        };

    protected override void ConfigureModuleTestServices(IServiceCollection services)
    {
        Repoint<CustomersContext>(services);
        Repoint<OrdersContext>(services);
    }

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<CustomersContext>().Database.Migrate();
        serviceProvider.GetRequiredService<OrdersContext>().Database.Migrate();
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await _rabbitMq.StartAsync();

        // Warm up the messaging path once so Wolverine's dynamic (Roslyn) handler codegen completes
        // before the assertions run, keeping the first real request fast.
        using var scope = Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var warmupCustomerId = PublicIdFactory.ForCustomer().Value.Value;
        try
        {
            await bus.InvokeAsync<OrdersForCustomerResponse>(new GetOrdersForCustomer(warmupCustomerId));
        }
        catch
        {
            // Ignore: warm-up only needs to trigger codegen.
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }

    private void Repoint<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));
    }
}
