using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShowRoom.Web.Client.Features.Catalog.ProductDetail;
using ShowRoom.Web.Client.Features.Catalog.ProductList;
using ShowRoom.Web.Client.Infrastructure.Api;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog;
using ShowRoom.Web.Shared.Api.Problems;
using ShowRoom.Web.Shared.PublicIds;

namespace ShowRoom.Web.Client.Features.Catalog;

/// <summary>
/// Orchestrates the Product HTTP surface for the catalogue screens: boundary validation, call through
/// the Refit client, translation of the status codes into an explicit outcome, and mapping to the view
/// model.
/// </summary>
/// <remarks>
/// <para>Runs on <b>both</b> sides — on the server while the page is prerendered, then in the browser.
/// It therefore holds nothing server-only: no <c>HttpContext</c>, no ambient state, only its injected
/// dependencies. A browser-side failure is also a different animal from a server-side one (CORS, a
/// blocked mixed-content call, an offline device), and they all land in the same explicit
/// <c>Unavailable</c> branch rather than as an unhandled exception in the middle of a render.</para>
/// </remarks>
public sealed class CatalogFacade(
    IProductApi productApi,
    IOptions<CatalogApiOptions> options,
    ILogger<CatalogFacade> logger) : ICatalogFacade
{
    private const string ListFeature = "ProductList";
    private const string DetailFeature = "ProductDetail";

    /// <summary>Upper bound applied to the requested page size, mirroring a sane server-side limit.</summary>
    internal const int MaxPageSize = 100;

    /// <summary>Page size used when the caller does not provide a valid one.</summary>
    internal const int DefaultPageSize = 12;

    public async Task<ProductListResult> GetProductsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Normalised here so a hand-edited query string can never turn into a 400 from the backend.
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;

        try
        {
            var response = await productApi.GetProductsAsync(
                options.Value.ApiVersion,
                normalizedPage,
                normalizedPageSize,
                cancellationToken);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                var view = ProductListMapper.FromApi(response.Content);

                logger.LogInformation(
                    "[{Feature}] Loaded page {Page} ({Count} of {TotalItems} products)",
                    ListFeature,
                    view.Page,
                    view.Items.Count,
                    view.TotalItems);

                return ProductListResult.Loaded(view);
            }

            var problem = ApiProblemReader.From(response);

            logger.LogError(
                response.Error,
                "[{Feature}] Unexpected status {StatusCode} while listing products ({Problem})",
                ListFeature,
                response.StatusCode,
                Describe(problem));

            return ProductListResult.Unavailable(problem);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // In the browser this also covers a CORS refusal and a blocked mixed-content call: both
            // surface as a transport failure with no status, which is exactly what "unavailable" means.
            logger.LogError(exception, "[{Feature}] Product surface unreachable while listing products", ListFeature);
            return ProductListResult.Unavailable();
        }
    }

    public async Task<ProductLookupResult> GetProductAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        if (!PublicIdFormat.IsValid(publicId))
        {
            logger.LogWarning("[{Feature}] Rejected malformed public id {PublicId}", DetailFeature, publicId);
            return ProductLookupResult.InvalidPublicId();
        }

        var normalized = publicId!.Trim();

        try
        {
            var response = await productApi.GetProductByPublicIdAsync(
                options.Value.ApiVersion,
                normalized,
                cancellationToken);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                logger.LogInformation("[{Feature}] Loaded product {PublicId}", DetailFeature, normalized);
                return ProductLookupResult.Found(ProductDetailMapper.FromApi(response.Content));
            }

            var problem = ApiProblemReader.From(response);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation(
                    "[{Feature}] No product found for {PublicId} ({Problem})",
                    DetailFeature,
                    normalized,
                    Describe(problem));

                return ProductLookupResult.NotFound(problem);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogWarning(
                    "[{Feature}] Backend rejected public id {PublicId} ({Problem})",
                    DetailFeature,
                    normalized,
                    Describe(problem));

                return ProductLookupResult.InvalidPublicId(problem);
            }

            logger.LogError(
                response.Error,
                "[{Feature}] Unexpected status {StatusCode} while loading product {PublicId} ({Problem})",
                DetailFeature,
                response.StatusCode,
                normalized,
                Describe(problem));

            return ProductLookupResult.Unavailable(problem);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[{Feature}] Product surface unreachable for {PublicId}", DetailFeature, normalized);
            return ProductLookupResult.Unavailable();
        }
    }

    /// <summary>
    /// Renders a problem as one readable log field: the codes are what a support conversation keys on,
    /// and the trace id ties the line back to the distributed trace of the failing call.
    /// </summary>
    private static string Describe(ApiProblem? problem)
    {
        if (problem is null)
        {
            return "no problem body";
        }

        var codes = problem.HasCodes ? string.Join(", ", problem.Codes) : "none";

        return $"{problem.Status} {problem.Title ?? "?"}; codes: {codes}; trace: {problem.TraceId ?? "none"}";
    }
}
