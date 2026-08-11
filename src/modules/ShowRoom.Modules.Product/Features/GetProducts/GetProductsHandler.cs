using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Pagination;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Product.Persistence;

namespace ShowRoom.Modules.Product.Features.GetProducts;

internal sealed class GetProductsHandler(
    ProductsContext context,
    ILogger<GetProductsHandler> logger)
    : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductSummaryResponse>>>
{
    private const string FeatureName = "GetProducts";
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<ProductSummaryResponse>>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(ProductModule.ModuleName, FeatureName, requestId);
        using var activity = ProductModule.ActivitySource.StartActivity("product.get_products");

        var (page, pageSize) = NormalizePagination(query.Page, query.PageSize);

        activity?
            .SetCommonTags(ProductModule.ModuleName, FeatureName, requestId)
            .SetTag("product.page", page)
            .SetTag("product.page_size", pageSize)
            .SetTag("product.has_public_id_filter", !string.IsNullOrWhiteSpace(query.PublicId));

        var productsQuery = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.PublicId))
        {
            if (!PublicId.TryParse(query.PublicId, out var publicId))
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Malformed product public id filter");
                logger.LogWarning("Rejecting product listing with malformed product public id filter");
                return Error.Validation(
                    "Product.InvalidPublicIdFilter",
                    "The product public id filter is not a valid public id.");
            }

            productsQuery = productsQuery.Where(product => product.PublicId == publicId!);
        }

        logger.LogInformation(
            "Listing products page {Page} size {PageSize} (public id filter: {HasFilter})",
            page,
            pageSize,
            !string.IsNullOrWhiteSpace(query.PublicId));

        var ordered = productsQuery.OrderByDescending(product => product.CreatedAt);

        var paged = await ordered.ToPagedResultAsync(
            page,
            pageSize,
            GetProductsAssembler.ToSummary,
            cancellationToken);

        activity?.SetTag("product.result.total_items", paged.TotalItems);
        logger.LogInformation("Retrieved {TotalItems} products ({ReturnedItems} on page {Page})",
            paged.TotalItems,
            paged.Items.Count,
            paged.Page);

        return Result<PagedResult<ProductSummaryResponse>>.Success(paged);
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}
