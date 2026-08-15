using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Observability;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.Modules.Customer.Contracts.Messaging;
using Wolverine;

namespace ShowRoom.Modules.Order.Features.OnCustomerRegistered;

/// <summary>
/// Wolverine message handler reacting to <see cref="CustomerRegisteredIntegrationEvent"/> published by the
/// Customer service over RabbitMQ. This is the cross-service consumer side of the transactional outbox:
/// the Order service reacts to a newly registered customer. In this POC the reaction is a logged
/// acknowledgement (a real module might seed a read model, prepare a welcome, …). A message that cannot be
/// tied to a customer is treated as poison and dead-lettered by the slice's retry policy
/// (see <see cref="OnCustomerRegisteredMessaging"/>).
/// </summary>
public sealed class CustomerRegisteredHandler
{
    private const string FeatureName = "OnCustomerRegistered";

    public void Handle(
        CustomerRegisteredIntegrationEvent message,
        Envelope envelope,
        ILogger<CustomerRegisteredHandler> logger)
    {
        var correlationId = envelope.Headers.GetValueOrDefault(MessageHeaders.CorrelationId)
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");
        var callerModule = envelope.Headers.GetValueOrDefault(MessageHeaders.ModuleName);
        var callerFeature = envelope.Headers.GetValueOrDefault(MessageHeaders.FeatureName);

        using var scope = logger.BeginModuleScope(OrderModule.ModuleName, FeatureName, correlationId);
        using var activity = OrderModule.ActivitySource.StartActivity("order.on_customer_registered");

        var stopwatch = Stopwatch.StartNew();

        activity?
            .SetCommonTags(OrderModule.ModuleName, FeatureName, correlationId)
            .SetTag("messaging.system", "rabbitmq")
            .SetTag("messaging.caller.module", callerModule)
            .SetTag("messaging.caller.feature", callerFeature)
            .SetTag("messaging.message_id", envelope.Headers.GetValueOrDefault(MessageHeaders.MessageId))
            .SetTag("messaging.attempt", envelope.Attempts) // retry attempt number — retries are visible on the trace
            .SetTag("customer.public_id", message.PublicId);

        // Poison guard: a message without a customer public id cannot be acted upon. Throwing routes it
        // through the retry/dead-letter policy rather than silently dropping it.
        if (string.IsNullOrWhiteSpace(message.PublicId))
        {
            var poison = new UnprocessableCustomerRegisteredException(message.Email);
            activity?.SetStatus(ActivityStatusCode.Error, "Unprocessable CustomerRegistered (missing public id)");
            activity?.AddException(poison);
            logger.LogWarning(
                poison,
                "Unprocessable CustomerRegistered message (missing public id) on attempt {Attempt}; it will be retried then dead-lettered",
                envelope.Attempts);
            RecordHandlerMetric(activity, stopwatch, "invalid_request");
            throw poison;
        }

        logger.LogInformation(
            "Reacting to newly registered customer {PublicId} ({Email})", message.PublicId, message.Email);

        RecordHandlerMetric(activity, stopwatch, "success");
    }

    private static void RecordHandlerMetric(Activity? activity, Stopwatch stopwatch, string outcome)
    {
        stopwatch.Stop();
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        activity?.SetTag("messaging.handler.duration_ms", elapsedMs);
        MessagingMetrics.RecordHandler(elapsedMs, OrderModule.ModuleName, FeatureName, outcome);
    }
}
