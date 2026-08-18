namespace ShowRoom.Web.Features.Customer.CustomerOrders;

/// <summary>
/// View model of the customer order-history screen. <see cref="OrdersAvailable"/> carries the
/// cross-service degradation: the customer is known, but the Order service could not be reached over
/// the bus — a partial success the UI must show as such, never as a plain error.
/// </summary>
public sealed record CustomerOrdersView(
    string PublicId,
    string DisplayName,
    string Email,
    bool OrdersAvailable,
    IReadOnlyList<CustomerOrderView> Orders)
{
    public bool HasOrders => Orders.Count > 0;

    /// <summary>True when the Order service answered and the customer simply has no order yet.</summary>
    public bool IsEmpty => OrdersAvailable && Orders.Count == 0;
}

/// <summary>One order of the history, display-ready.</summary>
public sealed record CustomerOrderView(
    string PublicId,
    string Status,
    string OrderDate,
    string TotalAmount,
    string ItemCount,
    IReadOnlyList<CustomerOrderLineView> Lines);

/// <summary>One product line of an order, display-ready.</summary>
public sealed record CustomerOrderLineView(
    string ProductPublicId,
    string ProductName,
    int Quantity,
    string UnitPrice,
    string LineTotal);
