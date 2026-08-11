using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ShowRoom.Modules.Order.Features.CreateOrder;

/// <summary>
/// Business metrics for order creation, emitted on the module Meter (<see cref="OrderModule"/>). These are
/// domain KPIs — <b>not</b> infrastructure timings — so they live with the feature that produces them:
/// <list type="bullet">
/// <item><c>showroom.orders.created</c> — counter of created orders (rate = orders/sec).</item>
/// <item><c>showroom.orders.amount</c> — histogram of order totals; <c>sum/count</c> gives the average
/// order value, and the buckets give the value distribution.</item>
/// <item><c>showroom.orders.items</c> — histogram of line-item count per order; <c>sum/count</c> gives
/// the average basket size in items.</item>
/// <item><c>showroom.orders.create.rejected</c> — counter of rejected creations, tagged by
/// <c>reason</c> (validation / domain).</item>
/// </list>
/// The value/created metrics are tagged by <c>order.currency</c> so amounts are never summed across
/// currencies.
/// </summary>
internal static class CreateOrderMetrics
{
    private static readonly Counter<long> OrdersCreated = OrderModule.Meter.CreateCounter<long>(
        "showroom.orders.created",
        unit: "{order}",
        description: "Number of orders created.");

    private static readonly Histogram<double> OrderAmount = OrderModule.Meter.CreateHistogram<double>(
        "showroom.orders.amount",
        description: "Monetary total of a created order, in its own currency.");

    private static readonly Histogram<int> OrderItems = OrderModule.Meter.CreateHistogram<int>(
        "showroom.orders.items",
        unit: "{item}",
        description: "Number of line items in a created order.");

    private static readonly Counter<long> Rejected = OrderModule.Meter.CreateCounter<long>(
        "showroom.orders.create.rejected",
        unit: "{rejection}",
        description: "Order creations rejected, by reason.");

    public static void RecordCreated(decimal totalAmount, string currency, int itemCount)
    {
        var tags = new TagList { { "order.currency", currency } };
        OrdersCreated.Add(1, tags);
        OrderAmount.Record((double)totalAmount, tags);
        OrderItems.Record(itemCount);
    }

    public static void RecordRejected(string reason)
        => Rejected.Add(1, new TagList { { "reason", reason } });
}
