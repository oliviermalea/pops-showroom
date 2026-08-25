using ShowRoom.Web.Features.Customer.CustomerOrders;
using ShowRoom.Web.Shared.Api.Problems;

namespace ShowRoom.Web.Features.Customer;

/// <summary>
/// Result of a customer order-history lookup. Note that a **partial** success (customer found, Order
/// service unreachable) is <see cref="CustomerLookupOutcome.Found"/> with
/// <c>OrdersAvailable = false</c> on the view — not a failure: the backend already degraded
/// gracefully, and the UI must relay that nuance instead of flattening it into an error.
/// </summary>
public sealed record CustomerOrdersResult(
    CustomerLookupOutcome Outcome,
    CustomerOrdersView? Customer,
    ApiProblem? Problem = null)
{
    public static CustomerOrdersResult Found(CustomerOrdersView customer) => new(CustomerLookupOutcome.Found, customer);

    public static CustomerOrdersResult InvalidPublicId(ApiProblem? problem = null)
        => new(CustomerLookupOutcome.InvalidPublicId, null, problem);

    public static CustomerOrdersResult NotFound(ApiProblem? problem = null)
        => new(CustomerLookupOutcome.NotFound, null, problem);

    public static CustomerOrdersResult Unavailable(ApiProblem? problem = null)
        => new(CustomerLookupOutcome.Unavailable, null, problem);
}
