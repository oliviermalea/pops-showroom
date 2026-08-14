using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Domain.Primitives;

namespace ShowRoom.BuildingBlocks.Application;

/// <summary>
/// Resolves and invokes the <see cref="IDomainEventHandler{TDomainEvent}"/> registered for each event.
/// Best-effort: a handler throwing is logged, never rethrown — the aggregate's transaction is already
/// committed by the time this runs, so a failing reaction must not fail the command. Guaranteed delivery
/// is the outbox's responsibility, not this dispatcher's.
/// </summary>
internal sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<DomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handle = handlerType.GetMethod("Handle")!;

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                try
                {
                    await (Task)handle.Invoke(handler, [domainEvent, cancellationToken])!;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Domain event handler {Handler} failed for {DomainEvent}",
                        handler.GetType().Name,
                        domainEvent.GetType().Name);
                }
            }
        }
    }
}
