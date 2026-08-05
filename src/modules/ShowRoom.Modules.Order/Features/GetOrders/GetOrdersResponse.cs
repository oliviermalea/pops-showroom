using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Order.Features.GetOrders;

/// <summary>A compact order representation for list views (no line detail, totals pre-computed).</summary>
public sealed record OrderSummaryResponse(
    PublicId PublicId,
    string CustomerPublicId,
    string Status,
    string Currency,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset CreatedAt);

/// <summary>Paginated list of order summaries.</summary>
public sealed record GetOrdersResponse(
    IReadOnlyCollection<OrderSummaryResponse> Orders,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
