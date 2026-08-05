using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Order.Features.CreateOrder;

/// <summary>
/// Minimal API endpoint (REPR): <c>POST /orders</c>. Returns 201 with a Location header pointing at
/// the new resource and the created <c>PublicId</c> as the body; 400 on validation — all as uniform
/// ProblemDetails.
/// </summary>
internal static class CreateOrderEndpoint
{
    internal static IEndpointRouteBuilder MapCreateOrder(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", HandleAsync)
            .WithName("CreateOrder")
            .WithSummary("Creates a new order and returns its public id.")
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        CreateOrderCommand command,
        ICommandHandler<CreateOrderCommand, Result<PublicId>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }

        var publicId = result.Value.Value;
        var urlPart = httpContext.GetUrlPart();
        var basePath = OrderConventions.BuildApiBasePath(urlPart.Version);
        var location = $"{urlPart.Scheme}://{urlPart.Host}{basePath}/{publicId}";

        return Results.Created(location, publicId);
    }
}
