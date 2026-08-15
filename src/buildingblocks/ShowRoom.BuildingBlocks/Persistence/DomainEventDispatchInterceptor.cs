using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.Primitives;

namespace ShowRoom.BuildingBlocks.Persistence;

/// <summary>
/// EF Core interceptor that dispatches aggregates' domain events AFTER a successful commit — keeping the
/// DbContext entirely free of event logic. It collects the events from the change tracker (via
/// <see cref="IHasDomainEvents"/>), clears them, then hands them to an <see cref="IDomainEventDispatcher"/>
/// resolved from a fresh DI scope. It NEVER writes to the context: dispatch is in-process and best-effort
/// (at-most-once). Anything requiring guaranteed cross-service delivery must be an integration event
/// published through the outbox, not this path.
/// </summary>
/// <remarks>
/// Registered as a <b>singleton</b> and dispatches through an <see cref="IServiceScopeFactory"/> scope: the
/// DbContext options (and therefore the attached interceptor) are built from the ROOT service provider when
/// the context is registered with Wolverine's outbox integration (<c>AddDbContextWithWolverineIntegration</c>),
/// so a scoped interceptor could not be resolved there. A dedicated scope also isolates the best-effort
/// reactions from the just-committed unit of work.
/// </remarks>
public sealed class DomainEventDispatchInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            var aggregates = context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Count > 0)
                .ToArray();

            if (aggregates.Length > 0)
            {
                var domainEvents = aggregates.SelectMany(entity => entity.DomainEvents).ToArray();

                // Clear BEFORE dispatch so a subsequent SaveChanges cannot re-emit the same events.
                foreach (var aggregate in aggregates)
                {
                    aggregate.ClearDomainEvents();
                }

                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
                await dispatcher.DispatchAsync(domainEvents, cancellationToken);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
