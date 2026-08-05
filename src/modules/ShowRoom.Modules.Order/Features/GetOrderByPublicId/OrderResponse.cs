using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

/// <summary>
/// Detailed order representation returned over HTTP. Exposes public identifiers only; internal
/// technical ids are never serialised. The customer and products are referenced by their public ids.
/// </summary>
public sealed record OrderResponse(
    PublicId PublicId,
    string CustomerPublicId,
    string Currency,
    string Status,
    decimal TotalAmount,
    IReadOnlyCollection<OrderLineResponse> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>A single line of the detailed order representation.</summary>
public sealed record OrderLineResponse(
    string ProductPublicId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
