using ShowRoom.Modules.Order.Contracts.Messaging;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Outbound port giving this slice a customer's order history. The history is obtained only through this
/// seam (an AMQP request/reply over the message bus) — never via HTTP or direct database access to the
/// Order data. Implementations may throw on transport failure/timeout; the caller decides the degradation
/// policy.
/// </summary>
public interface IOrderHistory
{
    Task<OrdersForCustomerResponse> ForCustomerAsync(
        string customerPublicId,
        CancellationToken cancellationToken = default);
}
