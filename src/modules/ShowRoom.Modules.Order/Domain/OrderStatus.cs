namespace ShowRoom.Modules.Order.Domain;

/// <summary>
/// Lifecycle status of an order. Modelled as a value object over a small closed set of values
/// (per the architecture rules), rather than a primitive string/int.
/// </summary>
public sealed record OrderStatus
{
    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Paid = new("Paid");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private OrderStatus(string value) => Value = value;

    public string Value { get; }

    public static OrderStatus FromValue(string value) => value switch
    {
        nameof(Pending) => Pending,
        nameof(Paid) => Paid,
        nameof(Cancelled) => Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown order status."),
    };

    public override string ToString() => Value;
}
