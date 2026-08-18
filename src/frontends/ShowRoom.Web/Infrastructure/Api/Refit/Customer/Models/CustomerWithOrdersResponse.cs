namespace ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

/// <summary>
/// Wire contract of <c>GET /api/v1/customers/{publicId}/with-orders</c>. The order history is
/// aggregated by the Customer service from the Order service **over AMQP**:
/// <see cref="OrdersAvailable"/> is <c>false</c> when that hop could not complete in time — the
/// customer is still returned, with an empty <see cref="Orders"/> collection (graceful degradation).
/// </summary>
public sealed record CustomerWithOrdersResponse(
    string PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? Phone,
    string Status,
    DateTimeOffset RegisteredOn,
    bool OrdersAvailable,
    IReadOnlyList<OrderHistoryLineResponse> Orders);

/// <summary>One order of the customer's history, with its full line detail.</summary>
public sealed record OrderHistoryLineResponse(
    string OrderPublicId,
    string Status,
    string Currency,
    DateTimeOffset OrderDate,
    decimal TotalAmount,
    IReadOnlyList<OrderLineDetailResponse> Lines);

/// <summary>A single product line of an order.</summary>
public sealed record OrderLineDetailResponse(
    string ProductPublicId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
