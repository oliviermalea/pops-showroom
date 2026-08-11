using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ShowRoom.BuildingBlocks.Observability;

/// <summary>
/// Custom OpenTelemetry metrics for the cross-service AMQP request/reply flow, complementing the
/// distributed trace (whose spans already give the per-element durations) and Wolverine's own metrics.
/// Two histograms measure the elapsed time of the two ends of the hop:
/// <list type="bullet">
/// <item><c>showroom.messaging.roundtrip.duration</c> — the full request/reply latency as perceived by
/// the <b>caller</b> (produce → reply received; includes broker + network + consumer time).</item>
/// <item><c>showroom.messaging.handler.duration</c> — the time spent handling the message on the
/// <b>consumer</b> (dequeue → reply produced).</item>
/// </list>
/// Register the meter with OpenTelemetry via <c>AddMeter(MessagingMetrics.MeterName)</c> in each host.
/// </summary>
public static class MessagingMetrics
{
    /// <summary>Meter name to register with OpenTelemetry (<c>AddMeter</c>).</summary>
    public const string MeterName = "ShowRoom.Messaging";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Histogram<double> RoundtripDuration = Meter.CreateHistogram<double>(
        "showroom.messaging.roundtrip.duration",
        unit: "ms",
        description: "AMQP request/reply round-trip latency as perceived by the caller.");

    private static readonly Histogram<double> HandlerDuration = Meter.CreateHistogram<double>(
        "showroom.messaging.handler.duration",
        unit: "ms",
        description: "Time spent handling an incoming AMQP message on the consumer.");

    private static readonly Counter<long> Retries = Meter.CreateCounter<long>(
        "showroom.messaging.retries",
        unit: "{retry}",
        description: "AMQP request/reply retry attempts (e.g. after a cold-start timeout).");

    /// <summary>Records a caller-side request/reply round-trip.</summary>
    public static void RecordRoundtrip(double elapsedMs, string module, string feature, string message, string outcome)
        => RoundtripDuration.Record(elapsedMs, new TagList
        {
            { "messaging.module", module },
            { "messaging.feature", feature },
            { "messaging.message", message },
            { "messaging.outcome", outcome },
        });

    /// <summary>Records a consumer-side message-handling duration.</summary>
    public static void RecordHandler(double elapsedMs, string module, string feature, string outcome)
        => HandlerDuration.Record(elapsedMs, new TagList
        {
            { "messaging.module", module },
            { "messaging.feature", feature },
            { "messaging.outcome", outcome },
        });

    /// <summary>Records a caller-side retry attempt (e.g. a cold-start timeout being retried).</summary>
    public static void RecordRetry(string module, string feature, string message)
        => Retries.Add(1, new TagList
        {
            { "messaging.module", module },
            { "messaging.feature", feature },
            { "messaging.message", message },
        });
}
