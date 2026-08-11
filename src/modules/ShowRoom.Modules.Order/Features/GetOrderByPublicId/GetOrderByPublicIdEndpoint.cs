using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

/// <summary>
/// Minimal API endpoint (REPR), mapped under the module route group (<c>/orders</c>). A malformed
/// <c>publicId</c> fails route binding and yields 400; a missing order yields 404 via ProblemDetails.
/// </summary>
public static class GetOrderByPublicIdEndpoint
{
    public static IEndpointRouteBuilder MapGetOrderByPublicId(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{publicId}", async (
                PublicId publicId,
                IQueryHandler<GetOrderByPublicIdQuery, Result<OrderResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetOrderByPublicIdQuery(publicId), cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : (IResult)result.ToProblemDetails();
            })
            .WithName("GetOrderByPublicId")
            .WithSummary("Gets an order by its public id.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
