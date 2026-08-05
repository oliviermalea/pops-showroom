using System.Diagnostics;

namespace ShowRoom.Modules.Customer.Observability;

internal static class CustomerTelemetry
{
    public const string SourceName = "ShowRoom.Modules.Customer";

    internal static readonly ActivitySource ActivitySource = new(SourceName);
}
