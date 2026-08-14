using ShowRoom.BuildingBlocks.Domain.Primitives;

namespace ShowRoom.BuildingBlocks.Application;

/// <summary>
/// Handles a domain event in-process. Implementations are resolved from DI and invoked (best-effort) by
/// the domain-event dispatch interceptor AFTER the aggregate's transaction has committed. A handler must
/// NOT write to the originating DbContext; to reach another service reliably, publish an integration
/// event through the outbox instead.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : DomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
