using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Customer detail plus their order history, aggregated across modules. <see cref="OrdersAvailable"/>
/// is <c>false</c> when the Order module could not be reached in time (reactive graceful degradation):
/// the customer is still returned, with an empty <see cref="Orders"/> collection.
/// </summary>
public sealed record CustomerWithOrdersResponse(
    PublicId PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? Phone,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool OrdersAvailable,
    IReadOnlyCollection<OrderHistoryLine> Orders);

/// <summary>One order in the customer's order history (a line of the history, public projection).</summary>
public sealed record OrderHistoryLine(
    string OrderPublicId,
    string Status,
    string Currency,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset CreatedAt);
