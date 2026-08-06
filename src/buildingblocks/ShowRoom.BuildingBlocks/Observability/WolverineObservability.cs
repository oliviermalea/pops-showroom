namespace ShowRoom.BuildingBlocks.Observability;

/// <summary>
/// Well-known OpenTelemetry names emitted by Wolverine, for registering messaging observability.
/// </summary>
public static class WolverineObservability
{
    /// <summary>ActivitySource Wolverine uses for send/handle spans (register via <c>AddSource</c>).</summary>
    public const string ActivitySourceName = "Wolverine";

    /// <summary>
    /// Meter name pattern for Wolverine metrics. Wolverine names its meter
    /// <c>"Wolverine:{ApplicationName}"</c>, so the wildcard is required for <c>AddMeter</c>.
    /// </summary>
    public const string MeterNamePattern = "Wolverine*";

    /// <summary>RabbitMQ.Client native ActivitySource for AMQP publish spans.</summary>
    public const string RabbitMqPublisherSourceName = "RabbitMQ.Client.Publisher";

    /// <summary>RabbitMQ.Client native ActivitySource for AMQP deliver/consume spans.</summary>
    public const string RabbitMqSubscriberSourceName = "RabbitMQ.Client.Subscriber";
}
