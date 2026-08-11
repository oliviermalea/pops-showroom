using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Product.Features.GetProductByPublicId;

/// <summary>
/// Minimal API endpoint (REPR), mapped under the module route group (<c>/products</c>). A malformed
/// <c>publicId</c> fails route binding and yields 400; a missing product yields 404 via ProblemDetails.
/// </summary>
public static class GetProductByPublicIdEndpoint
{
    public static IEndpointRouteBuilder MapGetProductByPublicId(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{publicId}", async (
                PublicId publicId,
                IQueryHandler<GetProductByPublicIdQuery, Result<ProductResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetProductByPublicIdQuery(publicId), cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : (IResult)result.ToProblemDetails();
            })
            .WithName("GetProductByPublicId")
            .WithSummary("Gets a product by its public id.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
