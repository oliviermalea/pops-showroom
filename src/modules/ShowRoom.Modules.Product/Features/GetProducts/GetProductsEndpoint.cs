using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>
/// Minimal API endpoint (REPR): <c>GET /products</c>. Returns a paginated list of product summaries,
/// optionally filtered by a name search term via <c>?search=</c>.
/// </summary>
internal static class GetProductsEndpoint
{
    internal static IEndpointRouteBuilder MapGetProducts(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", HandleAsync)
            .WithName("GetProducts")
            .WithSummary("Lists products (paginated), optionally filtered by a name search term.")
            .Produces<GetProductsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetProductsQuery, Result<GetProductsResponse>> handler,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            new GetProductsQuery(page, pageSize, search),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : (IResult)result.ToProblemDetails();
    }
}
