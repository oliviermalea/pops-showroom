using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Pagination;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>
/// Minimal API endpoint (REPR): <c>GET /products</c>. Returns a paginated list of product summaries,
/// optionally filtered to a single product by its public id via <c>?publicId=</c>.
/// </summary>
internal static class GetProductsEndpoint
{
    internal static IEndpointRouteBuilder MapGetProducts(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", HandleAsync)
            .WithName("GetProducts")
            .WithSummary("Lists products (paginated), optionally filtered by product public id.")
            .Produces<PagedResult<ProductSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetProductsQuery, Result<PagedResult<ProductSummaryResponse>>> handler,
        int page = 1,
        int pageSize = 20,
        string? publicId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            new GetProductsQuery(page, pageSize, publicId),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : (IResult)result.ToProblemDetails();
    }
}
