using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Http.Errors;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Features.GetCustomers;

/// <summary>
/// Minimal API endpoint (REPR): <c>GET /customers</c>. Returns a paginated list of customer summaries,
/// optionally filtered by a free-text search over the name via <c>?search=</c>.
/// </summary>
internal static class GetCustomersEndpoint
{
    internal static IEndpointRouteBuilder MapGetCustomers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", HandleAsync)
            .WithName("GetCustomers")
            .WithSummary("Lists customers (paginated), optionally filtered by a name search.")
            .Produces<GetCustomersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetCustomersQuery, Result<GetCustomersResponse>> handler,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            new GetCustomersQuery(page, pageSize, search),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : (IResult)result.ToProblemDetails();
    }
}
