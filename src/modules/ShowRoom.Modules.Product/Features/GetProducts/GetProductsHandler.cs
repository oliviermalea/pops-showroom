using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Pagination;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Product.Persistence;

namespace ShowRoom.Modules.Product.Features.GetProducts;

internal sealed class GetProductsHandler(
    ProductsContext context,
    ILogger<GetProductsHandler> logger)
    : IQueryHandler<GetProductsQuery, Result<GetProductsResponse>>
{
    private const string FeatureName = "GetProducts";
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<GetProductsResponse>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginModuleScope(ProductConventions.ModuleName, FeatureName);

        var (page, pageSize) = NormalizePagination(query.Page, query.PageSize);

        var productsQuery = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            productsQuery = productsQuery.Where(product => EF.Functions.ILike(product.Name, pattern));
        }

        logger.LogInformation(
            "Listing products page {Page} size {PageSize} (search: {HasSearch})",
            page,
            pageSize,
            !string.IsNullOrWhiteSpace(query.Search));

        var ordered = productsQuery.OrderByDescending(product => product.CreatedAt);

        var paged = await ordered.ToPagedResultAsync(
            page,
            pageSize,
            GetProductsAssembler.ToSummary,
            cancellationToken);

        logger.LogInformation("Retrieved {TotalItems} products ({ReturnedItems} on page {Page})",
            paged.TotalItems,
            paged.Items.Count,
            paged.Page);

        return Result<GetProductsResponse>.Success(new GetProductsResponse(
            paged.Items,
            paged.Page,
            paged.PageSize,
            paged.TotalItems,
            paged.TotalPages));
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}
