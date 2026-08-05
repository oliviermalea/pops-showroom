namespace ShowRoom.Modules.Customer.Domain;

/// <summary>
/// Binary customer status. Modelled as a value object (closed set of two values) per the
/// architecture rules, rather than a primitive string/int.
/// </summary>
public sealed record CustomerStatus
{
    public static readonly CustomerStatus Active = new("Active");
    public static readonly CustomerStatus Inactive = new("Inactive");

    private CustomerStatus(string value) => Value = value;

    public string Value { get; }

    public static CustomerStatus FromValue(string value) => value switch
    {
        nameof(Active) => Active,
        nameof(Inactive) => Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown customer status."),
    };

    public override string ToString() => Value;
}
