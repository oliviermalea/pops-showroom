using Asp.Versioning;

namespace ShowRoom.Modules.Product;

/// <summary>
/// Centralised naming/routing conventions for the Product module (name, tag, route, connection).
/// </summary>
public static class ProductConventions
{
    public const string ModuleName = "Product";
    public const string Tag = ModuleName;
    public const string RouteSegment = "products";
    public const string BaseRoute = "/" + RouteSegment;
    public const string ConnectionName = "productdb";

    public static string BuildApiBasePath(ApiVersion? version)
        => version is not null
            ? $"/api/v{version}{BaseRoute}"
            : $"/api{BaseRoute}";
}
