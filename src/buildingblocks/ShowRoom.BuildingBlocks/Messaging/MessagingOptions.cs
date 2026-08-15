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

    /// <summary>
    /// Enables Wolverine's transactional outbox for IntegrationEvents: a PostgreSQL message store persists
    /// outgoing envelopes in the SAME database transaction as the business change, then a durable sending
    /// agent delivers them to RabbitMQ with retries (guaranteed at-least-once cross-service delivery).
    /// The module keeps a SINGLE DbContext: the outbox writes envelopes through that context's own
    /// connection (that is what makes the write atomic), while the Wolverine tables live in their own
    /// <see cref="MessageStoreSchema"/> — logical separation without a second DbContext.
    /// </summary>
    public bool UseTransactionalOutbox { get; set; }

    /// <summary>
    /// Provisions the PostgreSQL message store WITHOUT the producer-side DbContext outbox integration —
    /// for a service that only needs the durable inbox + dead-letter storage (a consumer). Implied by
    /// <see cref="UseTransactionalOutbox"/>. Each service must use its own <see cref="MessageStoreSchema"/>
    /// (two Wolverine runtimes must not share one message store).
    /// </summary>
    public bool UsePersistentMessageStore { get; set; }

    /// <summary>
    /// Connection-string name for the message store. Defaults to the shared <c>showroom</c> database, so
    /// the Wolverine tables sit in the same database as the module (for the outbox this is required: it
    /// writes envelopes through the module DbContext's own connection).
    /// </summary>
    public string MessageStoreConnectionName { get; set; } = "showroom";

    /// <summary>
    /// Dedicated schema holding this service's Wolverine message-store tables, kept apart from module
    /// schemas AND from other services' stores (e.g. <c>wolverine</c> for Customer, <c>wolverine_business</c>
    /// for Business).
    /// </summary>
    public string MessageStoreSchema { get; set; } = "wolverine";

    /// <summary>Binds the options from the <c>"Messaging"</c> section (never null).</summary>
    public static MessagingOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetSection(SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
    }
}
