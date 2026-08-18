using ShowRoom.Web.Features.Customer.CustomerOrders;

namespace ShowRoom.Web.Features.Customer;

/// <summary>
/// Result of a customer order-history lookup. Note that a **partial** success (customer found, Order
/// service unreachable) is <see cref="CustomerLookupOutcome.Found"/> with
/// <c>OrdersAvailable = false</c> on the view — not a failure: the backend already degraded
/// gracefully, and the UI must relay that nuance instead of flattening it into an error.
/// </summary>
public sealed record CustomerOrdersResult(CustomerLookupOutcome Outcome, CustomerOrdersView? Customer)
{
    public static CustomerOrdersResult Found(CustomerOrdersView customer) => new(CustomerLookupOutcome.Found, customer);

    public static CustomerOrdersResult InvalidPublicId() => new(CustomerLookupOutcome.InvalidPublicId, null);

    public static CustomerOrdersResult NotFound() => new(CustomerLookupOutcome.NotFound, null);

    public static CustomerOrdersResult Unavailable() => new(CustomerLookupOutcome.Unavailable, null);
}
