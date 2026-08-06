using System.Diagnostics;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.Modules.Order.Contracts.Messaging;
using Wolverine;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Wolverine-backed <see cref="IOrderQueryGateway"/>. Uses <c>IMessageBus.InvokeAsync</c> request/reply:
/// the request is routed to a RabbitMQ queue (M2M over AMQP, not HTTP) and the reply is awaited
/// (Wolverine's default 5s remote-invocation timeout applies). The outgoing envelope carries the
/// standard <see cref="MessageHeaders"/> (module, feature, message id/type, correlation/trace id) so
/// the consumer can correlate and enrich its logs and spans.
/// </summary>
internal sealed class WolverineOrderQueryGateway(IMessageBus bus) : IOrderQueryGateway
{
    public Task<OrdersForCustomerResponse> GetOrdersForCustomerAsync(
        string customerPublicId,
        CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        var options = new DeliveryOptions()
            .WithHeader(MessageHeaders.ModuleName, CustomerModule.ModuleName)
            .WithHeader(MessageHeaders.FeatureName, nameof(GetOrdersForCustomer))
            .WithHeader(MessageHeaders.MessageType, nameof(GetOrdersForCustomer))
            .WithHeader(MessageHeaders.MessageId, Guid.NewGuid().ToString("N"))
            .WithHeader(MessageHeaders.CorrelationId, traceId)
            .WithHeader(MessageHeaders.TraceId, traceId);

        return bus.InvokeAsync<OrdersForCustomerResponse>(
            new GetOrdersForCustomer(customerPublicId),
            options,
            cancellationToken);
    }
}
