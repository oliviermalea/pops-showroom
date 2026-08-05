using Asp.Versioning;

namespace ShowRoom.Modules.Customer;

/// <summary>
/// Centralised naming/routing conventions for the Customer module (name, tag, route, connection).
/// </summary>
public static class CustomerConventions
{
    public const string ModuleName = "Customer";
    public const string Tag = ModuleName;
    public const string RouteSegment = "customers";
    public const string BaseRoute = "/" + RouteSegment;
    public const string ConnectionName = "customerdb";

    public static string BuildApiBasePath(ApiVersion? version)
        => version is not null
            ? $"/api/v{version}{BaseRoute}"
            : $"/api{BaseRoute}";
}
