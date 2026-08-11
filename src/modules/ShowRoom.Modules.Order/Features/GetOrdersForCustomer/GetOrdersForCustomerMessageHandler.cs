using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Observability;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.Modules.Order.Persistence;
using Wolverine;
// Alias: the message type shares its name with this slice's namespace segment.
using GetOrdersForCustomerRequest = ShowRoom.Modules.Order.Contracts.Messaging.GetOrdersForCustomer;

namespace ShowRoom.Modules.Order.Features.GetOrdersForCustomer;

/// <summary>
/// Wolverine message handler answering the cross-service <c>GetOrdersForCustomer</c> request over
/// RabbitMQ (AMQP request/reply). This is the Order module's machine-to-machine surface: other services
/// obtain order data through this message contract, never via HTTP or direct DB access. The returned
/// <see cref="OrdersForCustomerResponse"/> is sent back to the caller as the reply.
/// </summary>
public sealed class GetOrdersForCustomerMessageHandler
{
    private const string FeatureName = "GetOrdersForCustomer";

    public async Task<OrdersForCustomerResponse> Handle(
        GetOrdersForCustomerRequest message,
        Envelope envelope,
        OrdersContext context,
        ILogger<GetOrdersForCustomerMessageHandler> logger,
        CancellationToken cancellationToken)
    {
        // Read the standard headers set by the producer to correlate this consumer with the caller.
        // The correlation id falls back to the propagated trace id, then a fresh id.
        var correlationId = envelope.Headers.GetValueOrDefault(MessageHeaders.CorrelationId)
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");
        var callerModule = envelope.Headers.GetValueOrDefault(MessageHeaders.ModuleName);
        var callerFeature = envelope.Headers.GetValueOrDefault(MessageHeaders.FeatureName);

        using var scope = logger.BeginModuleScope(OrderModule.ModuleName, FeatureName, correlationId);
        using var activity = OrderModule.ActivitySource.StartActivity("order.get_orders_for_customer");

        // Elapsed time handling this message on the consumer side (dequeue → reply produced), recorded as
        // a metric and stamped on the span for the distributed trace.
        var stopwatch = Stopwatch.StartNew();

        activity?
            .SetCommonTags(OrderModule.ModuleName, FeatureName, correlationId)
            .SetTag("messaging.system", "rabbitmq")
            .SetTag("messaging.caller.module", callerModule)
            .SetTag("messaging.caller.feature", callerFeature)
            .SetTag("messaging.message_id", envelope.Headers.GetValueOrDefault(MessageHeaders.MessageId))
            .SetTag("order.customer.public_id", message.CustomerPublicId);

        if (!PublicId.TryParse(message.CustomerPublicId, out var customerPublicId))
        {
            activity?
                .SetStatus(ActivityStatusCode.Error, "Malformed customer public id")
                .SetTag("order.result.count", 0);
            logger.LogWarning("Received order query with malformed customer public id; returning empty result");
            RecordHandlerMetric(activity, stopwatch, "invalid_request");
            return new OrdersForCustomerResponse([]);
        }

        logger.LogInformation("Answering order query for customer {CustomerPublicId}", message.CustomerPublicId);

        var orders = await context.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.CustomerPublicId == customerPublicId!)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        activity?.SetTag("order.result.count", orders.Count);
        logger.LogInformation("Returning {Count} orders for customer {CustomerPublicId}",
            orders.Count,
            message.CustomerPublicId);

        RecordHandlerMetric(activity, stopwatch, "success");
        return GetOrdersForCustomerAssembler.From(orders);
    }

    private static void RecordHandlerMetric(Activity? activity, Stopwatch stopwatch, string outcome)
    {
        stopwatch.Stop();
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        activity?.SetTag("messaging.handler.duration_ms", elapsedMs);
        MessagingMetrics.RecordHandler(elapsedMs, OrderModule.ModuleName, FeatureName, outcome);
    }
}
