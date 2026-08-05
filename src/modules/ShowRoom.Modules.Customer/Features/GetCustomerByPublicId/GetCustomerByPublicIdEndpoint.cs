using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

/// <summary>
/// Minimal API endpoint (REPR), mapped under the module route group (<c>/customers</c>). A malformed
/// <c>publicId</c> fails route binding and yields 400; a missing customer yields 404 via ProblemDetails.
/// </summary>
public static class GetCustomerByPublicIdEndpoint
{
    public static IEndpointRouteBuilder MapGetCustomerByPublicId(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{publicId}", async (
                PublicId publicId,
                IQueryHandler<GetCustomerByPublicIdQuery, Result<CustomerResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetCustomerByPublicIdQuery(publicId), cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : (IResult)result.ToProblemDetails();
            })
            .WithName("GetCustomerByPublicId")
            .Produces<CustomerResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
