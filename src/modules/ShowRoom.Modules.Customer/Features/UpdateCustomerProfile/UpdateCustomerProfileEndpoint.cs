using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfile;

/// <summary>
/// Minimal API endpoint (REPR): <c>PUT /customers/{publicId}</c>. Full profile update; returns 204 on
/// success; 400 (validation), 404 (unknown customer) and 409 (email already used) as uniform ProblemDetails.
/// </summary>
internal static class UpdateCustomerProfileEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateCustomerProfile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{publicId}", HandleAsync)
            .WithName("UpdateCustomerProfile")
            .WithSummary("Updates a customer's profile (name, email, phone).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        string publicId,
        UpdateCustomerProfileRequest request,
        ICommandHandler<UpdateCustomerProfileCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateCustomerProfileCommand(publicId, request.FirstName, request.LastName, request.Email, request.Phone),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : (IResult)result.ToProblemDetails();
    }
}
