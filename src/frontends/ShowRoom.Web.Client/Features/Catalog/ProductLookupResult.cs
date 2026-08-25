using ShowRoom.Web.Client.Features.Catalog.ProductDetail;
using ShowRoom.Web.Shared.Api.Problems;

namespace ShowRoom.Web.Client.Features.Catalog;

/// <summary>Outcome of a product lookup, as the UI needs to distinguish it.</summary>
public enum ProductLookupOutcome
{
    /// <summary>The product was found.</summary>
    Found,

    /// <summary>The public id is not a well-formed ShowRoom public id (rejected before any call).</summary>
    InvalidPublicId,

    /// <summary>No product carries this public id (backend answered 404).</summary>
    NotFound,

    /// <summary>The Product surface could not be reached, or answered an unexpected status.</summary>
    Unavailable,
}

/// <summary>Result of a product lookup: an explicit outcome plus the view model when it succeeded.</summary>
public sealed record ProductLookupResult(
    ProductLookupOutcome Outcome,
    ProductDetailView? Product,
    ApiProblem? Problem = null)
{
    public static ProductLookupResult Found(ProductDetailView product) => new(ProductLookupOutcome.Found, product);

    public static ProductLookupResult InvalidPublicId(ApiProblem? problem = null)
        => new(ProductLookupOutcome.InvalidPublicId, null, problem);

    public static ProductLookupResult NotFound(ApiProblem? problem = null)
        => new(ProductLookupOutcome.NotFound, null, problem);

    public static ProductLookupResult Unavailable(ApiProblem? problem = null)
        => new(ProductLookupOutcome.Unavailable, null, problem);
}
