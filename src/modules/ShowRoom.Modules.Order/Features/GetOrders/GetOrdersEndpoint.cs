using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Pagination;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Order.Features.GetOrders;

/// <summary>
/// Minimal API endpoint (REPR): <c>GET /orders</c>. Returns a paginated list of order summaries,
/// optionally filtered to a single customer's history via <c>?customerPublicId=</c>.
/// </summary>
internal static class GetOrdersEndpoint
{
    internal static IEndpointRouteBuilder MapGetOrders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", HandleAsync)
            .WithName("GetOrders")
            .WithSummary("Lists orders (paginated), optionally filtered by customer public id.")
            .Produces<PagedResult<OrderSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetOrdersQuery, Result<PagedResult<OrderSummaryResponse>>> handler,
        int page = 1,
        int pageSize = 20,
        string? customerPublicId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            new GetOrdersQuery(page, pageSize, customerPublicId),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : (IResult)result.ToProblemDetails();
    }
}
