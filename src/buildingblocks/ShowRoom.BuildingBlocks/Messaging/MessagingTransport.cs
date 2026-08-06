using System.ComponentModel;
using Ardalis.SmartEnum;

namespace ShowRoom.BuildingBlocks.Messaging;

/// <summary>
/// The transport Wolverine uses for machine-to-machine messaging. <see cref="InMemory"/> keeps
/// everything in-process (no broker); <see cref="RabbitMq"/> routes over RabbitMQ (real distribution).
/// Bindable from the <c>"Messaging"</c> section by name via <see cref="MessagingTransportTypeConverter"/>.
/// </summary>
[TypeConverter(typeof(MessagingTransportTypeConverter))]
public sealed class MessagingTransport : SmartEnum<MessagingTransport>
{
    public static readonly MessagingTransport InMemory = new(nameof(InMemory), 0);
    public static readonly MessagingTransport RabbitMq = new(nameof(RabbitMq), 1);

    private MessagingTransport(string name, int value) : base(name, value) { }
}
