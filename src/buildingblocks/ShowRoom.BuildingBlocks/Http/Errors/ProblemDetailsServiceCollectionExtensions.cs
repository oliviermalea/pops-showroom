using Microsoft.Extensions.DependencyInjection;

namespace ShowRoom.BuildingBlocks.Http.Errors;

/// <summary>
/// Registers uniform ProblemDetails-based error handling shared by every ShowRoom API host, so a
/// malformed request body yields the same clean RFC 7807 <c>400</c> everywhere. Pair with
/// <c>app.UseExceptionHandler()</c> in the pipeline.
/// </summary>
public static class ProblemDetailsServiceCollectionExtensions
{
    public static IServiceCollection AddShowRoomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<BadRequestExceptionHandler>();

        return services;
    }
}
