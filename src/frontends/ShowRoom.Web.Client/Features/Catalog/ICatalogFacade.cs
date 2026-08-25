namespace ShowRoom.Web.Client.Features.Catalog;

/// <summary>
/// Single entry point of the Catalog module for the UI. Components never touch the typed API client:
/// they orchestrate this facade, which owns the call, the status handling and the mapping.
/// </summary>
public interface ICatalogFacade
{
    /// <summary>Loads one page of products.</summary>
    Task<ProductListResult> GetProductsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Loads a product detail by its public id.</summary>
    Task<ProductLookupResult> GetProductAsync(string? publicId, CancellationToken cancellationToken = default);
}
