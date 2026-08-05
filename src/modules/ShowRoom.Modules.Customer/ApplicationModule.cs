using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Application;

namespace ShowRoom.Modules.Customer;

/// <summary>Registers the Customer module's application handlers (commands/queries).</summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplicationModule(this IServiceCollection services)
    {
        services.AddApplicationHandlersFromAssembly(typeof(ApplicationModule).Assembly);

        return services;
    }
}
