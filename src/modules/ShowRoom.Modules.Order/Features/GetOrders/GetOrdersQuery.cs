namespace ShowRoom.Modules.Order.Features.GetOrders;

/// <summary>
/// Query: list orders, paginated and optionally filtered to a single customer's history
/// (by customer public id).
/// </summary>
public sealed record GetOrdersQuery(int Page, int PageSize, string? CustomerPublicId);
