using Microsoft.Extensions.Configuration;

namespace ShowRoom.BuildingBlocks.Messaging;

/// <summary>
/// Messaging configuration, bound from the <c>"Messaging"</c> configuration section. Lets each host
/// (and, through it, each module's messaging registration) turn messaging on/off and pick the transport
/// without code changes.
/// </summary>
public sealed class MessagingOptions
{
    /// <summary>Configuration section this binds from.</summary>
    public const string SectionName = "Messaging";

    /// <summary>When <c>false</c>, no transport is configured and modules skip their bus registrations.</summary>
    public bool Enabled { get; set; }

    /// <summary>Transport used by Wolverine. Defaults to RabbitMQ (ShowRoom is a distributed system).</summary>
    public MessagingTransport Transport { get; set; } = MessagingTransport.RabbitMq;

    /// <summary>Name of the RabbitMQ connection string / Aspire resource (default <c>messaging</c>).</summary>
    public string RabbitMqConnectionName { get; set; } = "messaging";

    /// <summary>
    /// Use durable local queues (requires a Wolverine message store). Off by default — ShowRoom does not
    /// configure Wolverine persistence, so enabling this without a store would fail at startup.
    /// </summary>
    public bool UseDurableLocalQueues { get; set; }

    /// <summary>Whether remote request/reply (<c>IMessageBus.InvokeAsync</c> across the broker) is allowed.</summary>
    public bool EnableRemoteInvocation { get; set; } = true;

    /// <summary>Binds the options from the <c>"Messaging"</c> section (never null).</summary>
    public static MessagingOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetSection(SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
    }
}
