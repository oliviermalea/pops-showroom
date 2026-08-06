using Microsoft.Extensions.Configuration;
using Wolverine;
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
