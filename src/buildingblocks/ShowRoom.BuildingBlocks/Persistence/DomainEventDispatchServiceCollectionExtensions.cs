using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShowRoom.BuildingBlocks.Application;

namespace ShowRoom.BuildingBlocks.Persistence;

/// <summary>
/// Registers the domain-event dispatch pipeline (dispatcher + EF interceptor), both scoped so the
/// interceptor and its handlers resolve per-DbContext instance. Idempotent — safe to call from every
/// module. Attach the interceptor to a context with
/// <c>options.AddInterceptors(sp.GetRequiredService&lt;DomainEventDispatchInterceptor&gt;())</c>.
/// </summary>
public static class DomainEventDispatchServiceCollectionExtensions
{
    public static IServiceCollection AddDomainEventDispatch(this IServiceCollection services)
    {
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.TryAddScoped<DomainEventDispatchInterceptor>();

        return services;
    }
}
