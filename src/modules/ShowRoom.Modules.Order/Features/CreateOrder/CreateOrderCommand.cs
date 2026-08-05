namespace ShowRoom.Modules.Order.Features.CreateOrder;

/// <summary>Command: create a new order. Payload of the create endpoint.</summary>
public sealed record CreateOrderCommand
{
    /// <summary>Public id of the customer the order belongs to (cross-module reference by value).</summary>
    public required string CustomerPublicId { get; init; }

    /// <summary>Optional 3-letter ISO currency code; defaults to EUR when omitted.</summary>
    public string? Currency { get; init; }

    public required IReadOnlyList<CreateOrderLine> Lines { get; init; }
}

/// <summary>A single line of the create-order payload.</summary>
public sealed record CreateOrderLine
{
    /// <summary>Public id of the ordered product (cross-module reference by value).</summary>
    public required string ProductPublicId { get; init; }

    /// <summary>Product name snapshot captured at order time.</summary>
    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }
}
