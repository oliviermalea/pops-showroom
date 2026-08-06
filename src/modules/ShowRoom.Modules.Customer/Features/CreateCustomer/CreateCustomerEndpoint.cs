using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

/// <summary>
/// Minimal API endpoint (REPR): <c>POST /customers</c>. Returns 201 with a Location header pointing
/// at the new resource and the created <c>PublicId</c> as the body; 400 on validation, 409 on
/// duplicate email — all as uniform ProblemDetails.
/// </summary>
internal static class CreateCustomerEndpoint
{
    internal static IEndpointRouteBuilder MapCreateCustomer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", HandleAsync)
            .WithName("CreateCustomer")
            .WithSummary("Creates a new customer and returns its public id.")
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        CreateCustomerCommand command,
        ICommandHandler<CreateCustomerCommand, Result<PublicId>> handler,
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
        var basePath = CustomerModule.BuildApiBasePath(urlPart.Version);
        var location = $"{urlPart.Scheme}://{urlPart.Host}{basePath}/{publicId}";

        return Results.Created(location, publicId);
    }
}
