using Microsoft.Extensions.Configuration;
using Npgsql;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace ShowRoom.BuildingBlocks.Messaging;

/// <summary>
/// Centralised Wolverine transport configuration for ShowRoom, driven by <see cref="MessagingOptions"/>
/// (the <c>"Messaging"</c> section). Hosts call this once inside <c>UseWolverine</c>; each module then
/// contributes its own routing/listeners/handlers through an <c>IWolverineExtension</c> registered by
/// its <c>MessagingModule.AddMessaging</c> (analogous to how persistence is registered per module).
/// </summary>
public static class WolverineMessagingExtensions
{
    public static WolverineOptions ConfigureShowRoomMessaging(
        this WolverineOptions options,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        var messaging = MessagingOptions.FromConfiguration(configuration);

        if (!messaging.Enabled)
        {
            return options;
        }

        if (!messaging.EnableRemoteInvocation)
        {
            options.EnableRemoteInvocation = false;
        }

        // PostgreSQL message store: the durable backbone for messaging. A PRODUCER
        // (Messaging:UseTransactionalOutbox) uses it to persist outgoing envelopes in the business
        // transaction (see AddDbContextWithOptionalOutbox); a CONSUMER (Messaging:UsePersistentMessageStore)
        // uses it for a durable inbox + dead-letter storage. Configured BEFORE the transport so durable
        // endpoints are backed by the store. The tables live in the dedicated per-service MessageStoreSchema.
        if (messaging.UseTransactionalOutbox || messaging.UsePersistentMessageStore)
        {
            var messageStoreConnectionString = configuration.GetConnectionString(messaging.MessageStoreConnectionName);
            ArgumentException.ThrowIfNullOrWhiteSpace(
                messageStoreConnectionString,
                $"ConnectionStrings:{messaging.MessageStoreConnectionName}");

            // The Wolverine durability agents poll this store on timers; with Npgsql command tracing enabled
            // each idle poll would emit an orphan span — a fresh trace every second in the dashboard even when
            // nothing happens. Disable tracing on the store's OWN data source so idle polling is silent, while
            // module DbContexts keep full query tracing (the atomic outbox write goes through the DbContext
            // connection, not this data source, so it stays visible in the business trace).
            var messageStoreDataSource = new NpgsqlDataSourceBuilder(messageStoreConnectionString)
                .ConfigureTracing(tracing => tracing
                    .ConfigureCommandFilter(_ => false)
                    .ConfigureBatchFilter(_ => false))
                .Build();

            options.PersistMessagesWithPostgresql(messageStoreDataSource, messaging.MessageStoreSchema);

            // Single instance per service in this topology: Solo mode skips the multi-node leadership /
            // agent-assignment / health-check polling that Balanced mode runs several times a second. Switch
            // back to Balanced when running multiple replicas of a service.
            options.Durability.Mode = DurabilityMode.Solo;
        }

        if (messaging.UseDurableLocalQueues)
        {
            options.Policies.UseDurableLocalQueues();
        }

        if (messaging.Transport == MessagingTransport.RabbitMq)
        {
            options
                .UseRabbitMqUsingNamedConnection(messaging.RabbitMqConnectionName)
                .AutoProvision();
        }

        // ShowRoom policy: modules register their message handlers explicitly (IncludeType); the REPR
        // IQueryHandler/ICommandHandler types must never be scanned as Wolverine message handlers.
        options.Discovery.DisableConventionalDiscovery();

        return options;
    }
}
