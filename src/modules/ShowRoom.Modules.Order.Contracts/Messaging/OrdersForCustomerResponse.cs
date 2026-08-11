namespace ShowRoom.Modules.Order.Contracts.Messaging;

/// <summary>Message-bus reply to <see cref="GetOrdersForCustomer"/>: the customer's orders.</summary>
public sealed record OrdersForCustomerResponse(IReadOnlyCollection<CustomerOrderSummary> Orders);

/// <summary>
/// A single order in the cross-module reply, with its full line detail. Carries only public ids and
/// denormalised values — no internal identifiers, no Order domain types leak across the boundary.
/// </summary>
public sealed record CustomerOrderSummary(
    string OrderPublicId,
    string Status,
    string Currency,
    DateTimeOffset OrderDate,
    decimal TotalAmount,
    IReadOnlyCollection<CustomerOrderLine> Lines);

/// <summary>A single product line of an order in the cross-module reply.</summary>
public sealed record CustomerOrderLine(
    string ProductPublicId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
