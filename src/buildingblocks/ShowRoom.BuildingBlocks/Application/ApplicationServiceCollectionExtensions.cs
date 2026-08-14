using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ShowRoom.BuildingBlocks.Application;

/// <summary>
/// Scrutor-based registration of a module's command and query handlers, scoped to a single assembly.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationHandlersFromAssemblyContaining<TAssemblyMarker>(this IServiceCollection services)
        => services.AddApplicationHandlersFromAssembly(typeof(TAssemblyMarker).Assembly);

    public static IServiceCollection AddApplicationHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
