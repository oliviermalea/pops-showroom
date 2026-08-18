using ShowRoom.Web.Features.Customer.CustomerDetail;

namespace ShowRoom.Web.Features.Customer;

/// <summary>Outcome of a customer lookup, as the UI needs to distinguish it.</summary>
public enum CustomerLookupOutcome
{
    /// <summary>The customer was found.</summary>
    Found,

    /// <summary>The public id is not a well-formed ShowRoom public id (rejected before any call).</summary>
    InvalidPublicId,

    /// <summary>No customer carries this public id (backend answered 404).</summary>
    NotFound,

    /// <summary>The Customer service could not be reached, or answered an unexpected status.</summary>
    Unavailable,
}

/// <summary>
/// Result of a customer lookup: an explicit outcome plus the view model when it succeeded. Modelled as
/// data rather than exceptions so each UI state (success / empty / error) maps to one branch.
/// </summary>
public sealed record CustomerLookupResult(CustomerLookupOutcome Outcome, CustomerDetailView? Customer)
{
    public static CustomerLookupResult Found(CustomerDetailView customer) => new(CustomerLookupOutcome.Found, customer);

    public static CustomerLookupResult InvalidPublicId() => new(CustomerLookupOutcome.InvalidPublicId, null);

    public static CustomerLookupResult NotFound() => new(CustomerLookupOutcome.NotFound, null);

    public static CustomerLookupResult Unavailable() => new(CustomerLookupOutcome.Unavailable, null);
}
