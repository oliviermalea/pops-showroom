namespace ShowRoom.Modules.Product.Domain;

/// <summary>
/// Binary availability status of a product. Modelled as a value object (closed set of two values)
/// per the architecture rules, rather than a primitive string/int.
/// </summary>
public sealed record ProductStatus
{
    public static readonly ProductStatus Available = new("Available");
    public static readonly ProductStatus Discontinued = new("Discontinued");

    private ProductStatus(string value) => Value = value;

    public string Value { get; }

    public static ProductStatus FromValue(string value) => value switch
    {
        nameof(Available) => Available,
        nameof(Discontinued) => Discontinued,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown product status."),
    };

    public override string ToString() => Value;
}
