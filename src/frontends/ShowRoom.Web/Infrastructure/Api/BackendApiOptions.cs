namespace ShowRoom.Web.Infrastructure.Api;

/// <summary>
/// Base addresses of the backend services consumed over HTTP, bound from the <c>BackendApi</c>
/// configuration section. Under Aspire the value is a service-discovery scheme
/// (<c>https+http://showroom-customer-api</c>); in a standalone run it is the local endpoint.
/// </summary>
public sealed class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    /// <summary>Base address of <c>ShowRoom.Customer.Api</c>.</summary>
    public required string CustomerApiBaseUrl { get; init; }

    /// <summary>API version segment used when building routes (<c>/api/v{version}/...</c>).</summary>
    public int ApiVersion { get; init; } = 1;
}
