using ShowRoom.Web.Features.Customer.CustomerList;
using ShowRoom.Web.Shared.Api.Problems;

namespace ShowRoom.Web.Features.Customer;

/// <summary>Outcome of a customer list query.</summary>
public enum CustomerListOutcome
{
    /// <summary>The page was loaded (it may legitimately contain no item).</summary>
    Loaded,

    /// <summary>The Customer service could not be reached, or answered an unexpected status.</summary>
    Unavailable,
}

/// <summary>Result of a customer list query: an explicit outcome plus the page when it succeeded.</summary>
public sealed record CustomerListResult(
    CustomerListOutcome Outcome,
    CustomerListView? Page,
    ApiProblem? Problem = null)
{
    public static CustomerListResult Loaded(CustomerListView page) => new(CustomerListOutcome.Loaded, page);

    public static CustomerListResult Unavailable(ApiProblem? problem = null)
        => new(CustomerListOutcome.Unavailable, null, problem);
}
