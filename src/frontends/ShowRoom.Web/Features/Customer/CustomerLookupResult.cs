using ShowRoom.Web.Features.Customer.CustomerDetail;
using ShowRoom.Web.Infrastructure.Api.Problems;

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
public sealed record CustomerLookupResult(
    CustomerLookupOutcome Outcome,
    CustomerDetailView? Customer,
    ApiProblem? Problem = null)
{
    public static CustomerLookupResult Found(CustomerDetailView customer) => new(CustomerLookupOutcome.Found, customer);

    public static CustomerLookupResult InvalidPublicId(ApiProblem? problem = null)
        => new(CustomerLookupOutcome.InvalidPublicId, null, problem);

    public static CustomerLookupResult NotFound(ApiProblem? problem = null)
        => new(CustomerLookupOutcome.NotFound, null, problem);

    public static CustomerLookupResult Unavailable(ApiProblem? problem = null)
        => new(CustomerLookupOutcome.Unavailable, null, problem);
}
