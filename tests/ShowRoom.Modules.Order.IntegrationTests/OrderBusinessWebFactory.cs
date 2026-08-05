namespace ShowRoom.Modules.Order.IntegrationTests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Modules.Order.Persistence;
using ShowRoom.Testing;

/// <summary>
/// Order module test factory: repoints <see cref="OrdersContext"/> at the factory's isolated
/// PostgreSQL container and migrates it, giving each run a clean, isolated database.
/// </summary>
public sealed class OrderBusinessWebFactory : BusinessWebFactory
{
    protected override void ConfigureModuleTestServices(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(DbContextOptions<OrdersContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<OrdersContext>(options =>
            options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));
    }

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<OrdersContext>()
            .Database
            .Migrate();
    }
}
