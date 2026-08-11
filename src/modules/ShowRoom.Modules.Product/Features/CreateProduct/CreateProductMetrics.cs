using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ShowRoom.Modules.Product.Features.CreateProduct;

/// <summary>
/// Business metric for product creation, emitted on the module Meter (<see cref="ProductModule"/>):
/// <c>showroom.products.create.rejected</c> — counter of rejected creations, tagged by <c>reason</c>
/// (validation / domain), i.e. the failure side of catalog entry. (No "created" counter — catalog growth
/// volume is rarely watched; the rejection signal is the actionable one.)
/// </summary>
internal static class CreateProductMetrics
{
    private static readonly Counter<long> Rejected = ProductModule.Meter.CreateCounter<long>(
        "showroom.products.create.rejected",
        unit: "{rejection}",
        description: "Product creations rejected, by reason.");

    public static void RecordRejected(string reason)
        => Rejected.Add(1, new TagList { { "reason", reason } });
}
