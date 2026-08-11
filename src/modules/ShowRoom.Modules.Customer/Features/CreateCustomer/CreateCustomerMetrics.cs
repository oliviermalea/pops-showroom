using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

/// <summary>
/// Business metrics for customer creation, emitted on the module Meter (<see cref="CustomerModule"/>):
/// <list type="bullet">
/// <item><c>showroom.customers.registered</c> — counter of registered customers (acquisition rate).</item>
/// <item><c>showroom.customers.create.rejected</c> — counter of rejected creations, tagged by
/// <c>reason</c> (validation / conflict), i.e. the failure side of the sign-up funnel.</item>
/// </list>
/// </summary>
internal static class CreateCustomerMetrics
{
    private static readonly Counter<long> Registered = CustomerModule.Meter.CreateCounter<long>(
        "showroom.customers.registered",
        unit: "{customer}",
        description: "Number of customers registered.");

    private static readonly Counter<long> Rejected = CustomerModule.Meter.CreateCounter<long>(
        "showroom.customers.create.rejected",
        unit: "{rejection}",
        description: "Customer creations rejected, by reason.");

    public static void RecordRegistered() => Registered.Add(1);

    public static void RecordRejected(string reason)
        => Rejected.Add(1, new TagList { { "reason", reason } });
}
