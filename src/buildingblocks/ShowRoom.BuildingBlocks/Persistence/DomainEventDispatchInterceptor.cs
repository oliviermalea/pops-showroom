using Microsoft.EntityFrameworkCore.Diagnostics;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.Primitives;

namespace ShowRoom.BuildingBlocks.Persistence;

/// <summary>
/// EF Core interceptor that dispatches aggregates' domain events AFTER a successful commit — keeping the
/// DbContext entirely free of event logic. It collects the events from the change tracker (via
/// <see cref="IHasDomainEvents"/>), clears them, then hands them to the injected
/// <see cref="IDomainEventDispatcher"/>. It NEVER writes to the context: dispatch is in-process and
/// best-effort (at-most-once). Anything requiring guaranteed cross-service delivery must be an integration
/// event published through the outbox, not this path.
/// </summary>
public sealed class DomainEventDispatchInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
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

                await dispatcher.DispatchAsync(domainEvents, cancellationToken);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
