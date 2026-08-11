using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Order.Features.GetOrders;

/// <summary>
/// A compact order representation for list views (no line detail, totals pre-computed). The paginated
/// list is returned as the shared <see cref="ShowRoom.BuildingBlocks.Application.Pagination.PagedResult{T}"/>.
/// </summary>
public sealed record OrderSummaryResponse(
    PublicId PublicId,
    string CustomerPublicId,
    string Status,
    string Currency,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset OrderDate);
