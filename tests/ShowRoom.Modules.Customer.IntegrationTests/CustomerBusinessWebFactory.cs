namespace ShowRoom.Modules.Customer.IntegrationTests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Testing;

/// <summary>
/// Customer module test factory: repoints <see cref="CustomersContext"/> at the factory's isolated
/// PostgreSQL container and migrates it, giving each run a clean, isolated database.
/// </summary>
public sealed class CustomerBusinessWebFactory : BusinessWebFactory
{
    protected override void ConfigureModuleTestServices(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(DbContextOptions<CustomersContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<CustomersContext>(options =>
            options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));
    }

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<CustomersContext>()
            .Database
            .Migrate();
    }
}
