using System.Diagnostics;

namespace ShowRoom.Modules.Product.Observability;

internal static class ProductTelemetry
{
    public const string SourceName = "ShowRoom.Modules.Product";

    internal static readonly ActivitySource ActivitySource = new(SourceName);
}
