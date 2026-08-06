using ShowRoom.Modules.Order.Contracts.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// This slice's Wolverine routing, applied automatically as an <see cref="IWolverineExtension"/>:
/// <see cref="GetOrdersForCustomer"/> is published to the Order service's RabbitMQ queue (request/reply).
/// The transport itself is configured centrally by <c>ConfigureShowRoomMessaging</c>.
/// </summary>
internal sealed class GetCustomerWithOrdersMessaging : IWolverineExtension
{
    public void Configure(WolverineOptions options)
        => options.PublishMessage<GetOrdersForCustomer>()
            .ToRabbitQueue(OrderMessagingContract.GetOrdersForCustomerQueue);
}
