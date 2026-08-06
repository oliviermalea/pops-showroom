using System.ComponentModel;
using System.Globalization;

namespace ShowRoom.BuildingBlocks.Messaging;

/// <summary>
/// Lets the configuration binder convert a string (e.g. <c>"RabbitMq"</c>) from the <c>"Messaging"</c>
/// section into a <see cref="MessagingTransport"/> SmartEnum, which otherwise has no string binding.
/// </summary>
public sealed class MessagingTransportTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string name
            ? MessagingTransport.FromName(name, ignoreCase: true)
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
        => destinationType == typeof(string) && value is MessagingTransport transport
            ? transport.Name
            : base.ConvertTo(context, culture, value, destinationType);
}
