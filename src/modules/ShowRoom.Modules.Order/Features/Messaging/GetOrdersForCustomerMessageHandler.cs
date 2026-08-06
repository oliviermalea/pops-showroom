using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.Modules.Order.Persistence;

namespace ShowRoom.Modules.Order.Features.Messaging;

/// <summary>
/// Wolverine message handler answering the cross-module <see cref="GetOrdersForCustomer"/> request
/// over RabbitMQ (AMQP request/reply). This is the Order module's machine-to-machine surface: other
/// modules obtain order data through this message contract, never via HTTP or direct DB access.
/// The returned <see cref="OrdersForCustomerResponse"/> is sent back to the caller as the reply.
///
/// The host binds this handler *sticky* to the RabbitMQ listener endpoint (see UseWolverine), so it is
/// NOT a global in-process handler — <c>IMessageBus.InvokeAsync</c> therefore cannot execute it inline
/// and the request genuinely crosses the broker (producer span on the caller, consumer span here),
/// even though both modules currently share one process.
/// </summary>
public sealed class GetOrdersForCustomerMessageHandler
{
    private const string FeatureName = "GetOrdersForCustomer";

    public async Task<OrdersForCustomerResponse> Handle(
        GetOrdersForCustomer message,
        OrdersContext context,
        ILogger<GetOrdersForCustomerMessageHandler> logger,
        CancellationToken cancellationToken)
    {
        // Wolverine created the ambient handling span and propagated the trace context from the caller
        // across RabbitMQ, so this TraceId correlates both sides of the message boundary.
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(OrderConventions.ModuleName, FeatureName, requestId);

        Activity.Current?
            .SetCommonTags(OrderConventions.ModuleName, FeatureName, requestId)
            .SetTag("messaging.system", "rabbitmq")
            .SetTag("order.customer.public_id", message.CustomerPublicId);

        if (!PublicId.TryParse(message.CustomerPublicId, out var customerPublicId))
        {
            logger.LogWarning("Received order query with malformed customer public id; returning empty result");
            Activity.Current?.SetTag("order.result.count", 0);
            return new OrdersForCustomerResponse([]);
        }

        logger.LogInformation("Answering order query for customer {CustomerPublicId}", message.CustomerPublicId);

        var orders = await context.Orders
            .AsNoTracking()
            .Where(order => order.CustomerPublicId == customerPublicId!)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        Activity.Current?.SetTag("order.result.count", orders.Count);
        logger.LogInformation("Returning {Count} orders for customer {CustomerPublicId}",
            orders.Count,
            message.CustomerPublicId);

        return GetOrdersForCustomerAssembler.From(orders);
    }
}
