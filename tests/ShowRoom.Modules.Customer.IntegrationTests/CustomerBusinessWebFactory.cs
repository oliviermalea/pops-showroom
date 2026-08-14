namespace ShowRoom.Modules.Customer.IntegrationTests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Persistence;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Testing;

/// <summary>
/// Customer module test factory: repoints <see cref="CustomersContext"/> at the factory's isolated
/// PostgreSQL container and migrates it, giving each run a clean, isolated database.
/// </summary>
public sealed class CustomerBusinessWebFactory : BusinessWebFactory<Program>
{
    protected override void ConfigureModuleTestServices(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(DbContextOptions<CustomersContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        // Re-attach the domain-event dispatch interceptor so the test host matches production behaviour
        // (repointing the DbContext at the container would otherwise drop it).
        services.AddDbContext<CustomersContext>((provider, options) =>
            options.UseNpgsql(_postgreSqlContainer.GetConnectionString())
                .AddInterceptors(provider.GetRequiredService<DomainEventDispatchInterceptor>()));
    }

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<CustomersContext>()
            .Database
            .Migrate();
    }
}
