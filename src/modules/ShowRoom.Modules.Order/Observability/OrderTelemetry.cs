using System.Diagnostics;

namespace ShowRoom.Modules.Order.Observability;

internal static class OrderTelemetry
{
    public const string SourceName = "ShowRoom.Modules.Order";

    internal static readonly ActivitySource ActivitySource = new(SourceName);
}
