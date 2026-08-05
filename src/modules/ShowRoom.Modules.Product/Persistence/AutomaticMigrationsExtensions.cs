using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ShowRoom.Modules.Product.Persistence;

internal static class AutomaticMigrationsExtensions
{
    /// <summary>Applies any pending database changes for the Product module at startup.</summary>
    internal static IApplicationBuilder UseAutomaticMigrations(this IApplicationBuilder applicationBuilder)
    {
        using var scope = applicationBuilder.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductsContext>();
        context.Database.Migrate();

        return applicationBuilder;
    }
}
