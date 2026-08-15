using ShowRoom.Modules.Customer.Contracts.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

/// <summary>
/// This slice's Wolverine routing, applied automatically as an <see cref="IWolverineExtension"/>:
/// <see cref="CustomerRegisteredIntegrationEvent"/> is published to a <b>durable</b> RabbitMQ queue.
/// Combined with the Postgres message store (see <c>ConfigureShowRoomMessaging</c>), delivery is guaranteed
/// at-least-once: the outgoing envelope is persisted in the customer-insert transaction (via the handler's
/// <c>IDbContextOutbox</c>) and the durable sending agent retries it until acknowledged — surviving process
/// crashes and broker outages. The transport itself is configured centrally by <c>ConfigureShowRoomMessaging</c>.
/// </summary>
internal sealed class CreateCustomerMessaging : IWolverineExtension
{
    public void Configure(WolverineOptions options)
        => options.PublishMessage<CustomerRegisteredIntegrationEvent>()
            .ToRabbitQueue(CustomerMessagingContract.CustomerRegisteredQueue)
            .UseDurableOutbox();
}
