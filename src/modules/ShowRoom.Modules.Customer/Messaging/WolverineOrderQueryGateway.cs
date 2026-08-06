using ShowRoom.Modules.Order.Contracts.Messaging;
using Wolverine;

namespace ShowRoom.Modules.Customer.Messaging;

/// <summary>
/// Wolverine-backed <see cref="IOrderQueryGateway"/>. Uses <c>IMessageBus.InvokeAsync</c> request/reply:
/// the request is routed to a RabbitMQ queue (M2M over AMQP, not HTTP) and the reply is awaited
/// (Wolverine's default 5s remote-invocation timeout applies).
/// </summary>
internal sealed class WolverineOrderQueryGateway(IMessageBus bus) : IOrderQueryGateway
{
    public Task<OrdersForCustomerResponse> GetOrdersForCustomerAsync(
        string customerPublicId,
        CancellationToken cancellationToken = default)
        => bus.InvokeAsync<OrdersForCustomerResponse>(
            new GetOrdersForCustomer(customerPublicId),
            cancellationToken);
}
