using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShowRoom.BuildingBlocks.Application;

namespace ShowRoom.BuildingBlocks.Persistence;

/// <summary>
/// Registers the domain-event dispatch pipeline: a scoped <see cref="IDomainEventDispatcher"/> (resolves
/// the per-request handlers) and a singleton <see cref="DomainEventDispatchInterceptor"/> (the DbContext
/// options are built from the root provider under Wolverine's outbox integration, so the interceptor must
/// be root-resolvable; it opens its own scope to dispatch). Idempotent — safe to call from every module.
/// Attach the interceptor to a context with
/// <c>options.AddInterceptors(sp.GetRequiredService&lt;DomainEventDispatchInterceptor&gt;())</c>.
/// </summary>
public static class DomainEventDispatchServiceCollectionExtensions
{
    public static IServiceCollection AddDomainEventDispatch(this IServiceCollection services)
    {
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.TryAddSingleton<DomainEventDispatchInterceptor>();

        return services;
    }
}
