using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;

/// <summary>
/// Minimal API endpoint (REPR): <c>PATCH /customers/{publicId}/email</c>. Returns 204 on success; 400
/// (validation), 404 (unknown customer) and 409 (email already used) as uniform ProblemDetails.
/// </summary>
internal static class ChangeCustomerEmailEndpoint
{
    internal static IEndpointRouteBuilder MapChangeCustomerEmail(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/{publicId}/email", HandleAsync)
            .WithName("ChangeCustomerEmail")
            .WithSummary("Changes a customer's email address.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        string publicId,
        ChangeCustomerEmailRequest request,
        ICommandHandler<ChangeCustomerEmailCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ChangeCustomerEmailCommand(publicId, request.NewEmail),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : (IResult)result.ToProblemDetails();
    }
}
