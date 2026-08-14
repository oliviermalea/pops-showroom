using ShowRoom.BuildingBlocks.Domain.Primitives;

namespace ShowRoom.BuildingBlocks.Application;

/// <summary>Dispatches domain events to their registered <see cref="IDomainEventHandler{TDomainEvent}"/>.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<DomainEvent> domainEvents, CancellationToken cancellationToken);
}
