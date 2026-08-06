namespace ShowRoom.Modules.Product.IntegrationTests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Modules.Product.Persistence;
using ShowRoom.Testing;

/// <summary>
/// Product module test factory: repoints <see cref="ProductsContext"/> at the factory's isolated
/// PostgreSQL container and migrates it, giving each run a clean, isolated database.
/// </summary>
public sealed class ProductBusinessWebFactory : BusinessWebFactory<Program>
{
    protected override void ConfigureModuleTestServices(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(DbContextOptions<ProductsContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<ProductsContext>(options =>
            options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));
    }

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<ProductsContext>()
            .Database
            .Migrate();
    }
}
