using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Observability;
using ShowRoom.Modules.Order.Contracts.Messaging;
using Wolverine;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Messaging-backed <see cref="IOrderHistory"/>. Uses <c>IMessageBus.InvokeAsync</c> request/reply: the
/// request is routed to a RabbitMQ queue (M2M over AMQP, not HTTP) and the reply is awaited (Wolverine's
/// default 5s remote-invocation timeout applies). The outgoing envelope carries the standard
/// <see cref="MessageHeaders"/> (module, feature, message id/type, correlation/trace id) so the consumer
/// can correlate and enrich its logs and spans.
///
/// <para>The <b>first</b> request after startup often times out: Wolverine establishes the RabbitMQ
/// connection and provisions the queues (including the reply queue) lazily on first use, which can exceed
/// the 5s timeout. Because this is an idempotent read, a bounded retry on <see cref="TimeoutException"/>
/// absorbs that cold start — the next attempt hits an already-warm path and returns in milliseconds —
/// instead of surfacing as a spurious "Order module unavailable" degradation.</para>
/// </summary>
internal sealed class MessagingOrderHistory(IMessageBus bus, ILogger<MessagingOrderHistory> logger) : IOrderHistory
{
    private const string FeatureName = "GetCustomerWithOrders";
    private const int MaxAttempts = 3;

    public async Task<OrdersForCustomerResponse> ForCustomerAsync(
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

        var request = new GetOrdersForCustomer(customerPublicId);

        // Measure the caller-perceived AMQP round-trip (produce → reply received) as a metric, so it can
        // be aggregated (p50/p95/p99) on top of the per-span durations already in the distributed trace.
        var stopwatch = Stopwatch.StartNew();
        var outcome = "success";
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await bus.InvokeAsync<OrdersForCustomerResponse>(request, options, cancellationToken);
                }
                catch (TimeoutException) when (attempt < MaxAttempts)
                {
                    // Cold start: broker connection / reply queue still warming up. Retry the idempotent
                    // query with a short backoff; the warm attempt returns quickly.
                    logger.LogWarning(
                        "AMQP request/reply timed out (attempt {Attempt}/{MaxAttempts}); retrying after broker warm-up",
                        attempt,
                        MaxAttempts);
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                }
            }
        }
        catch
        {
            outcome = "failure";
            throw;
        }
        finally
        {
            stopwatch.Stop();
            MessagingMetrics.RecordRoundtrip(
                stopwatch.Elapsed.TotalMilliseconds,
                CustomerModule.ModuleName,
                FeatureName,
                nameof(GetOrdersForCustomer),
                outcome);
        }
    }
}
