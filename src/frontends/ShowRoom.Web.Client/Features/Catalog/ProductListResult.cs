using ShowRoom.Web.Client.Features.Catalog.ProductList;
using ShowRoom.Web.Shared.Api.Problems;

namespace ShowRoom.Web.Client.Features.Catalog;

/// <summary>Outcome of a product list query.</summary>
public enum ProductListOutcome
{
    /// <summary>The page was loaded (it may legitimately contain no product).</summary>
    Loaded,

    /// <summary>The Product surface could not be reached, or answered an unexpected status.</summary>
    Unavailable,
}

/// <summary>Result of a product list query: an explicit outcome, the page, and the API problem if any.</summary>
public sealed record ProductListResult(
    ProductListOutcome Outcome,
    ProductListView? Page,
    ApiProblem? Problem = null)
{
    public static ProductListResult Loaded(ProductListView page) => new(ProductListOutcome.Loaded, page);

    public static ProductListResult Unavailable(ApiProblem? problem = null)
        => new(ProductListOutcome.Unavailable, null, problem);
}
