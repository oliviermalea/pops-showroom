using Asp.Versioning;

namespace ShowRoom.Modules.Order;

/// <summary>
/// Centralised naming/routing conventions for the Order module (name, tag, route, connection).
/// </summary>
public static class OrderConventions
{
    public const string ModuleName = "Order";
    public const string Tag = ModuleName;
    public const string RouteSegment = "orders";
    public const string BaseRoute = "/" + RouteSegment;
    public const string ConnectionName = "orderdb";

    public static string BuildApiBasePath(ApiVersion? version)
        => version is not null
            ? $"/api/v{version}{BaseRoute}"
            : $"/api{BaseRoute}";
}
