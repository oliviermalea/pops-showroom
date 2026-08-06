using ShowRoom.Modules.Order.Contracts.Messaging;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Outbound anti-corruption gateway to the Order service. This slice obtains order data only through
/// this seam (an AMQP request/reply over the message bus) — never via HTTP or direct database access to
/// the Order data. Implementations may throw on transport failure/timeout; the caller decides the
/// degradation policy.
/// </summary>
public interface IOrderQueryGateway
{
    Task<OrdersForCustomerResponse> GetOrdersForCustomerAsync(
        string customerPublicId,
        CancellationToken cancellationToken = default);
}
