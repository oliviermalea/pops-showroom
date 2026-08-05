namespace ShowRoom.Modules.Product.Features.CreateProduct;

/// <summary>Command: create a new product. Payload of the create endpoint.</summary>
public sealed record CreateProductCommand
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required decimal Price { get; init; }

    /// <summary>Optional 3-letter ISO currency code; defaults to EUR when omitted.</summary>
    public string? Currency { get; init; }
}
