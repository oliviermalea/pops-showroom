using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.Modules.Order.Observability;
using ShowRoom.Modules.Order.Persistence;

namespace ShowRoom.Modules.Order.Features.Messaging;

/// <summary>
/// Wolverine message handler answering the cross-service <see cref="GetOrdersForCustomer"/> request
/// over RabbitMQ (AMQP request/reply). This is the Order module's machine-to-machine surface: other
/// services obtain order data through this message contract, never via HTTP or direct DB access. The
/// returned <see cref="OrdersForCustomerResponse"/> is sent back to the caller as the reply. The request
/// originates in ShowRoom.Customer.Api, so it genuinely crosses the broker.
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
        // Wolverine propagated the trace context from the caller across RabbitMQ, so this TraceId
        // correlates both sides of the message boundary; the span below is a child of the consumer span.
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(OrderConventions.ModuleName, FeatureName, requestId);
        using var activity = OrderTelemetry.ActivitySource.StartActivity("order.get_orders_for_customer");

        activity?
            .SetCommonTags(OrderConventions.ModuleName, FeatureName, requestId)
            .SetTag("messaging.system", "rabbitmq")
            .SetTag("order.customer.public_id", message.CustomerPublicId);

        if (!PublicId.TryParse(message.CustomerPublicId, out var customerPublicId))
        {
            activity?
                .SetStatus(ActivityStatusCode.Error, "Malformed customer public id")
                .SetTag("order.result.count", 0);
            logger.LogWarning("Received order query with malformed customer public id; returning empty result");
            return new OrdersForCustomerResponse([]);
        }

        logger.LogInformation("Answering order query for customer {CustomerPublicId}", message.CustomerPublicId);

        var orders = await context.Orders
            .AsNoTracking()
            .Where(order => order.CustomerPublicId == customerPublicId!)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        activity?.SetTag("order.result.count", orders.Count);
        logger.LogInformation("Returning {Count} orders for customer {CustomerPublicId}",
            orders.Count,
            message.CustomerPublicId);

        return GetOrdersForCustomerAssembler.From(orders);
    }
}
