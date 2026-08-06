using ShowRoom.Modules.Order.Contracts.Messaging;

namespace ShowRoom.Modules.Customer.Messaging;

/// <summary>
/// Outbound anti-corruption gateway to the Order module. The Customer module obtains order data only
/// through this seam (an AMQP request/reply over the message bus) — never via HTTP or direct database
/// access to the Order module. Implementations may throw on transport failure/timeout; callers decide
/// the degradation policy.
/// </summary>
public interface IOrderQueryGateway
{
    Task<OrdersForCustomerResponse> GetOrdersForCustomerAsync(
        string customerPublicId,
        CancellationToken cancellationToken = default);
}
