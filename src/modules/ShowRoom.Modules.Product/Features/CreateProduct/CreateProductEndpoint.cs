using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Product.Features.CreateProduct;

/// <summary>
/// Minimal API endpoint (REPR): <c>POST /products</c>. Returns 201 with a Location header pointing at
/// the new resource and the created <c>PublicId</c> as the body; 400 on validation — all as uniform
/// ProblemDetails.
/// </summary>
internal static class CreateProductEndpoint
{
    internal static IEndpointRouteBuilder MapCreateProduct(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", HandleAsync)
            .WithName("CreateProduct")
            .WithSummary("Creates a new product and returns its public id.")
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        CreateProductCommand command,
        ICommandHandler<CreateProductCommand, Result<PublicId>> handler,
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
        var basePath = ProductConventions.BuildApiBasePath(urlPart.Version);
        var location = $"{urlPart.Scheme}://{urlPart.Host}{basePath}/{publicId}";

        return Results.Created(location, publicId);
    }
}
