using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ShowRoom.Modules.Customer.Persistence;

internal static class AutomaticMigrationsExtensions
{
    /// <summary>
    /// Uses automatic migrations to apply any pending database changes.
    /// </summary>
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <returns>The updated <see cref="IApplicationBuilder"/> instance.</returns>
    internal static IApplicationBuilder UseAutomaticMigrations(this IApplicationBuilder applicationBuilder)
    {
        using var scope = applicationBuilder.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CustomersContext>();
        context.Database.Migrate();

        return applicationBuilder;
    }
}
