using Refit;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;
using ShowRoom.Web.Shared.Api.Models;

namespace ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog;

/// <summary>
/// Typed client over the Product surface of ShowRoom.Business.Api.
/// </summary>
/// <remarks>
/// Same contract as the server-side clients: methods return <see cref="ApiResponse{T}"/>, so a status
/// code carrying a business outcome is handled as data by the facade rather than as an exception.
/// </remarks>
[Headers("Accept: application/json")]
public interface IProductApi
{
    /// <summary>Lists products (paginated).</summary>
    [Get("/api/v{version}/products")]
    Task<ApiResponse<PagedResponse<ProductSummaryResponse>>> GetProductsAsync(
        int version,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a product by its public id.</summary>
    [Get("/api/v{version}/products/{publicId}")]
    Task<ApiResponse<ProductResponse>> GetProductByPublicIdAsync(
        int version,
        string publicId,
        CancellationToken cancellationToken = default);
}
