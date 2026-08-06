using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Minimal API endpoint (REPR): <c>GET /customers/{publicId}/with-orders</c>. Returns the customer
/// together with their order history (fetched from the Order module over AMQP). A missing customer
/// yields 404; if the Order module is unavailable the customer is still returned with
/// <c>ordersAvailable = false</c>.
/// </summary>
public static class GetCustomerWithOrdersEndpoint
{
    public static IEndpointRouteBuilder MapGetCustomerWithOrders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{publicId}/with-orders", async (
                PublicId publicId,
                IQueryHandler<GetCustomerWithOrdersQuery, Result<CustomerWithOrdersResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetCustomerWithOrdersQuery(publicId), cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : (IResult)result.ToProblemDetails();
            })
            .WithName("GetCustomerWithOrders")
            .WithSummary("Gets a customer with their order history (orders fetched from the Order module over AMQP).")
            .Produces<CustomerWithOrdersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
