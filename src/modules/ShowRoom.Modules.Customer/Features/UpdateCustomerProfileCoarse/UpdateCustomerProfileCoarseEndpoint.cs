using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfileCoarse;

/// <summary>
/// Minimal API endpoint (REPR): <c>PUT /customers/{publicId}/profile</c>. The "Style 2" variant of the
/// profile update (single <c>CustomerProfileUpdated</c> event), co-existing with <c>PUT /{publicId}</c>
/// (Style 1) purely for side-by-side comparison.
/// </summary>
internal static class UpdateCustomerProfileCoarseEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateCustomerProfileCoarse(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{publicId}/profile", HandleAsync)
            .WithName("UpdateCustomerProfileCoarse")
            .WithSummary("Updates a customer's profile as one operation (single profile-updated event).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        string publicId,
        UpdateCustomerProfileCoarseRequest request,
        ICommandHandler<UpdateCustomerProfileCoarseCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateCustomerProfileCoarseCommand(publicId, request.FirstName, request.LastName, request.Email, request.Phone),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : (IResult)result.ToProblemDetails();
    }
}
